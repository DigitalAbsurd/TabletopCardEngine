<#
.SYNOPSIS
    Lists the public surface of TabletopCardEngine.dll as Markdown, for docs/public-api.md.

.DESCRIPTION
    Loads the built library into an isolated AssemblyLoadContext, resolving its package
    dependencies through obj/project.assets.json, and walks every exported type grouped by
    namespace: the type with the first sentence of its XML doc summary, then each public or
    protected member with the first sentence of its own summary. A positional record property
    has no summary of its own, so its <param> text on the record is used instead.

    Compiler-generated record plumbing (Equals, GetHashCode, ToString, PrintMembers, Deconstruct,
    EqualityContract, <Clone>$, the == and != operators) is left out: it is part of the binary
    surface, and package validation still checks it, but it follows from the record declaration
    and would only bury the members a reviewer cares about.

    Build the library first; `dotnet pack src/TabletopCardEngine -c Release` does. Without -Update
    the inventory is printed. With -Update it replaces the text between the two GENERATED markers
    in docs/public-api.md.

.EXAMPLE
    pwsh ./scripts/list-public-api.ps1 -Update
#>
[CmdletBinding()]
param(
    [string] $AssemblyPath,
    [switch] $Update
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectDir = Join-Path $repoRoot 'src/TabletopCardEngine'
if (-not $AssemblyPath) {
    $AssemblyPath = Join-Path $projectDir 'bin/Release/net10.0/TabletopCardEngine.dll'
}
if (-not (Test-Path $AssemblyPath)) {
    throw "$AssemblyPath not found. Run 'dotnet pack src/TabletopCardEngine -c Release' first."
}
$AssemblyPath = (Resolve-Path $AssemblyPath).Path
$xmlPath = [IO.Path]::ChangeExtension($AssemblyPath, '.xml')

# Package dependencies are not copied next to a class library, so find them in the NuGet folders.
$assets = Get-Content -Raw (Join-Path $projectDir 'obj/project.assets.json') | ConvertFrom-Json
$packageFolders = @($assets.packageFolders.PSObject.Properties.Name)
$dependencyPaths = @{}
foreach ($target in $assets.targets.PSObject.Properties) {
    foreach ($library in $target.Value.PSObject.Properties) {
        $libraryPath = $assets.libraries.($library.Name).path
        foreach ($group in @($library.Value.runtime, $library.Value.compile)) {
            if (-not $group) {
                continue
            }
            foreach ($file in $group.PSObject.Properties.Name) {
                if (-not $file.EndsWith('.dll')) {
                    continue
                }
                foreach ($folder in $packageFolders) {
                    $candidate = Join-Path $folder (Join-Path $libraryPath $file)
                    if (Test-Path $candidate) {
                        $dependencyPaths[[IO.Path]::GetFileNameWithoutExtension($file)] = $candidate
                        break
                    }
                }
            }
        }
    }
}

$context = [Runtime.Loader.AssemblyLoadContext]::new('list-public-api', $true)
$context.add_Resolving({
        param($loadContext, $name)
        $path = $dependencyPaths[$name.Name]
        if ($path) {
            return $loadContext.LoadFromAssemblyPath($path)
        }
        return $null
    })
$assembly = $context.LoadFromAssemblyPath($AssemblyPath)

# XML documentation, keyed by documentation ID.
$docs = @{}
if (Test-Path $xmlPath) {
    foreach ($member in ([xml](Get-Content -Raw $xmlPath)).doc.members.member) {
        $docs[$member.name] = $member
    }
}

function Get-DocTypeName([Type] $type) {
    if ($type.IsGenericParameter) {
        if ($type.DeclaringMethod) {
            return '``' + $type.GenericParameterPosition
        }
        return '`' + $type.GenericParameterPosition
    }
    if ($type.IsByRef) {
        return (Get-DocTypeName $type.GetElementType()) + '@'
    }
    if ($type.IsArray) {
        return (Get-DocTypeName $type.GetElementType()) + '[]'
    }
    if ($type.IsGenericType -and -not $type.IsGenericTypeDefinition) {
        $definition = (Get-DocTypeName $type.GetGenericTypeDefinition()) -replace '`\d+$', ''
        $arguments = ($type.GetGenericArguments() | ForEach-Object { Get-DocTypeName $_ }) -join ','
        return "$definition{$arguments}"
    }
    if ($type.IsNested) {
        return (Get-DocTypeName $type.DeclaringType) + '.' + $type.Name
    }
    return "$($type.Namespace).$($type.Name)"
}

function Get-DocId($member) {
    if ($member -is [Type]) {
        return 'T:' + (Get-DocTypeName $member)
    }
    $owner = Get-DocTypeName $member.DeclaringType
    if ($member -is [Reflection.PropertyInfo]) {
        return "P:$owner.$($member.Name)"
    }
    if ($member -is [Reflection.FieldInfo]) {
        return "F:$owner.$($member.Name)"
    }
    if ($member -is [Reflection.EventInfo]) {
        return "E:$owner.$($member.Name)"
    }
    $name = if ($member.IsConstructor) { '#ctor' } else { $member.Name }
    if ($member.IsGenericMethod) {
        $name += '``' + $member.GetGenericArguments().Length
    }
    $parameters = @($member.GetParameters())
    if ($parameters.Length -eq 0) {
        return "M:$owner.$name"
    }
    $list = ($parameters | ForEach-Object { Get-DocTypeName $_.ParameterType }) -join ','
    return "M:$owner.$name($list)"
}

function Get-FirstSentence([string] $text) {
    $flat = ($text -replace '\s+', ' ').Trim()
    # A sentence ends at a full stop before a capital letter or the end, so "e.g. 1" does not end one.
    $match = [regex]::Match($flat, '^.*?\.(?=\s+[A-Z`]|$)')
    if ($match.Success) {
        return $match.Value
    }
    return $flat
}

function Get-DocText($node) {
    if (-not $node) {
        return ''
    }
    # Turn <see cref="T:X.Y"/> and friends into the bare name, and drop the remaining tags.
    $xml = $node.InnerXml
    $xml = [regex]::Replace($xml, '<see(?:also)? (?:cref|langword|href)="(?:[A-Z]:)?([^"(]+)[^"]*"\s*/>', {
            param($m)
            '`' + ($m.Groups[1].Value -split '\.')[-1] + '`'
        })
    $xml = [regex]::Replace($xml, '<(?:paramref|typeparamref) name="([^"]+)"\s*/>', '`$1`')
    $xml = [regex]::Replace($xml, '</?c>', '`')
    $xml = [regex]::Replace($xml, '<[^>]+>', '')
    return Get-FirstSentence ([Net.WebUtility]::HtmlDecode($xml))
}

function Get-Purpose($member) {
    $doc = $docs[(Get-DocId $member)]
    $summary = if ($doc) { $doc.SelectSingleNode('summary') }
    if ($summary) {
        return Get-DocText $summary
    }
    if ($member -is [Reflection.PropertyInfo]) {
        # A positional record property is documented by the record's <param>.
        $typeDoc = $docs[(Get-DocId $member.DeclaringType)]
        if ($typeDoc) {
            $param = $typeDoc.SelectSingleNode("param[@name='$($member.Name)']")
            if ($param) {
                return Get-DocText $param
            }
        }
    }
    if ($member -is [Reflection.ConstructorInfo] -and $member.GetParameters().Length -eq 0) {
        return 'The implicit parameterless constructor, for object initializers.'
    }
    return '*(undocumented)*'
}

$keywords = @{
    'System.Boolean' = 'bool'; 'System.Byte' = 'byte'; 'System.Char' = 'char'; 'System.Decimal' = 'decimal'
    'System.Double' = 'double'; 'System.Int16' = 'short'; 'System.Int32' = 'int'; 'System.Int64' = 'long'
    'System.Object' = 'object'; 'System.SByte' = 'sbyte'; 'System.Single' = 'float'; 'System.String' = 'string'
    'System.UInt16' = 'ushort'; 'System.UInt32' = 'uint'; 'System.UInt64' = 'ulong'; 'System.Void' = 'void'
}

$nullability = [Reflection.NullabilityInfoContext]::new()

# $info carries the nullable reference annotations, which the Type itself does not.
function Format-Type([Type] $type, [Reflection.NullabilityInfo] $info) {
    if ($type.IsByRef) {
        return Format-Type $type.GetElementType() $info
    }
    $suffix = ''
    if ($info -and -not $type.IsValueType -and $info.ReadState -eq 'Nullable') {
        $suffix = '?'
    }
    if ($type.IsArray) {
        $element = if ($info) { $info.ElementType }
        return (Format-Type $type.GetElementType() $element) + '[]' + $suffix
    }
    if ($type.IsGenericType -and $type.GetGenericTypeDefinition().FullName -eq 'System.Nullable`1') {
        return (Format-Type $type.GetGenericArguments()[0]) + '?'
    }
    if ($type.IsGenericType) {
        $name = $type.Name -replace '`\d+$', ''
        $typeArguments = $type.GetGenericArguments()
        $arguments = for ($i = 0; $i -lt $typeArguments.Length; $i++) {
            $argumentInfo = if ($info -and $info.GenericTypeArguments.Length -gt $i) { $info.GenericTypeArguments[$i] }
            Format-Type $typeArguments[$i] $argumentInfo
        }
        return "$name<$($arguments -join ', ')>$suffix"
    }
    if ($type.FullName -and $keywords.ContainsKey($type.FullName)) {
        return $keywords[$type.FullName] + $suffix
    }
    return $type.Name + $suffix
}

function Format-Parameters($method) {
    $parts = foreach ($parameter in $method.GetParameters()) {
        $modifier = if ($parameter.IsOut) { 'out ' } elseif ($parameter.ParameterType.IsByRef) { 'ref ' } else { '' }
        $text = "$modifier$(Format-Type $parameter.ParameterType $nullability.Create($parameter)) $($parameter.Name)"
        if ($parameter.HasDefaultValue) {
            $value = $parameter.RawDefaultValue
            $text += ' = ' + $(if ($null -eq $value) { if ($parameter.ParameterType.IsValueType -and -not [Nullable]::GetUnderlyingType($parameter.ParameterType)) { 'default' } else { 'null' } } elseif ($value -is [string]) { "`"$value`"" } elseif ($value -is [bool]) { "$value".ToLowerInvariant() } else { "$value" })
        }
        $text
    }
    return $parts -join ', '
}

function Get-TypeKind([Type] $type) {
    if ($type.IsEnum) {
        return 'enum'
    }
    if ($type.IsInterface) {
        return 'interface'
    }
    $isRecord = $null -ne $type.GetMethod('<Clone>$')
    if ($type.IsValueType) {
        $kind = if ($isRecord) { 'record struct' } else { 'struct' }
        if ($type.GetCustomAttributesData() | Where-Object { $_.AttributeType.Name -eq 'IsReadOnlyAttribute' }) {
            $kind = "readonly $kind"
        }
        return $kind
    }
    $kind = if ($isRecord) { 'record' } else { 'class' }
    if ($type.IsAbstract -and $type.IsSealed) {
        return "static $kind"
    }
    if ($type.IsAbstract) {
        return "abstract $kind"
    }
    if ($type.IsSealed) {
        return "sealed $kind"
    }
    return $kind
}

function Test-Visible($member) {
    return $member.IsPublic -or $member.IsFamily -or $member.IsFamilyOrAssembly
}

function Test-CompilerGenerated($member) {
    return [bool]($member.GetCustomAttributesData() | Where-Object { $_.AttributeType.Name -eq 'CompilerGeneratedAttribute' })
}

$flags = [Reflection.BindingFlags]'Public, NonPublic, Instance, Static, DeclaredOnly'

function Get-MemberLines([Type] $type) {
    if ($type.IsEnum) {
        foreach ($field in $type.GetFields([Reflection.BindingFlags]'Public, Static')) {
            "- ``$($field.Name) = $($field.GetRawConstantValue())`` — $(Get-Purpose $field)"
        }
        return
    }
    foreach ($constructor in $type.GetConstructors($flags) | Where-Object { (Test-Visible $_) -and -not $_.IsStatic }) {
        $parameters = @($constructor.GetParameters())
        # The protected copy constructor of a record is plumbing.
        if ($parameters.Length -eq 1 -and $parameters[0].ParameterType -eq $type -and $type.GetMethod('<Clone>$')) {
            continue
        }
        "- ``$($type.Name -replace '`\d+$', '')($(Format-Parameters $constructor))`` — $(Get-Purpose $constructor)"
    }
    foreach ($property in $type.GetProperties($flags) | Sort-Object MetadataToken) {
        $accessors = @($property.GetMethod, $property.SetMethod) | Where-Object { $_ -and (Test-Visible $_) }
        if (-not $accessors -or $property.Name -eq 'EqualityContract') {
            continue
        }
        $static = if ($accessors[0].IsStatic) { 'static ' } else { '' }
        $parts = foreach ($accessor in $accessors) {
            if ($accessor -eq $property.GetMethod) {
                'get;'
            }
            elseif ($accessor.ReturnParameter.GetRequiredCustomModifiers() | Where-Object { $_.Name -eq 'IsExternalInit' }) {
                'init;'
            }
            else {
                'set;'
            }
        }
        "- ``$static$(Format-Type $property.PropertyType $nullability.Create($property)) $($property.Name) { $($parts -join ' ') }`` — $(Get-Purpose $property)"
    }
    foreach ($field in $type.GetFields($flags) | Where-Object { Test-Visible $_ } | Sort-Object MetadataToken) {
        $modifier = if ($field.IsLiteral) { 'const ' } elseif ($field.IsStatic) { 'static readonly ' } else { '' }
        "- ``$modifier$(Format-Type $field.FieldType $nullability.Create($field)) $($field.Name)`` — $(Get-Purpose $field)"
    }
    $methods = $type.GetMethods($flags) | Where-Object {
        (Test-Visible $_) -and -not $_.IsSpecialName -and -not (Test-CompilerGenerated $_) -and $_.Name -ne '<Clone>$'
    }
    $operators = $type.GetMethods($flags) | Where-Object {
        $_.IsPublic -and $_.IsSpecialName -and $_.Name.StartsWith('op_') -and -not (Test-CompilerGenerated $_)
    }
    foreach ($method in @($methods) + @($operators) | Where-Object { $_ } | Sort-Object MetadataToken) {
        $static = if ($method.IsStatic) { 'static ' } else { '' }
        $generic = ''
        if ($method.IsGenericMethod) {
            $generic = '<' + (($method.GetGenericArguments() | ForEach-Object Name) -join ', ') + '>'
        }
        "- ``$static$(Format-Type $method.ReturnType $nullability.Create($method.ReturnParameter)) $($method.Name)$generic($(Format-Parameters $method))`` — $(Get-Purpose $method)"
    }
}

$lines = [Collections.Generic.List[string]]::new()
$types = $assembly.GetExportedTypes() | Sort-Object Namespace, Name
foreach ($namespace in $types | Group-Object Namespace) {
    $lines.Add("### ``$($namespace.Name)``")
    $lines.Add('')
    foreach ($type in $namespace.Group) {
        $bases = @()
        if ($type.BaseType -and -not $type.IsEnum -and -not $type.IsValueType -and $type.BaseType.FullName -ne 'System.Object') {
            $bases += Format-Type $type.BaseType
        }
        $bases += $type.GetInterfaces() | Where-Object { -not $type.IsEnum -and $_.IsPublic -and $_.Name -notlike 'IEquatable*' } | ForEach-Object { Format-Type $_ }
        $heritage = if ($bases) { ' : ' + ($bases -join ', ') } else { '' }
        $lines.Add("#### ``$(Format-Type $type)$heritage`` ($(Get-TypeKind $type))")
        $lines.Add('')
        $lines.Add((Get-Purpose $type))
        $lines.Add('')
        $members = @(Get-MemberLines $type)
        if ($members.Length -gt 0) {
            $lines.AddRange([string[]]$members)
            $lines.Add('')
        }
    }
}
$inventory = ($lines -join "`n").TrimEnd() + "`n"
$context.Unload()

if (-not $Update) {
    $inventory
    return
}

$docPath = Join-Path $repoRoot 'docs/public-api.md'
$begin = '<!-- BEGIN GENERATED by scripts/list-public-api.ps1 -->'
$end = '<!-- END GENERATED -->'
$document = (Get-Content -Raw $docPath) -replace "`r`n", "`n"
$start = $document.IndexOf($begin)
$stop = $document.IndexOf($end)
if ($start -lt 0 -or $stop -lt $start) {
    throw "docs/public-api.md has no '$begin' ... '$end' region."
}
$document = $document.Substring(0, $start + $begin.Length) + "`n`n" + $inventory + "`n" + $document.Substring($stop)
[IO.File]::WriteAllText($docPath, $document, [Text.UTF8Encoding]::new($false))

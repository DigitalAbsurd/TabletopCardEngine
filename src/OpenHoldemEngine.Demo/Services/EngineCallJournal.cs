using FluentResults;
using OpenHoldemEngine.Dto;

namespace OpenHoldemEngine.Demo.Services;

/// <summary>
/// An ordered log of engine calls and their results.
/// Designed to be inspected and replayed, not merely displayed.
/// </summary>
public sealed class EngineCallJournal
{
    private readonly List<EngineCallJournalEntry> _entries = [];

    /// <summary>Gets the ordered list of all engine calls made through this journal.</summary>
    public IReadOnlyList<EngineCallJournalEntry> Entries => _entries.AsReadOnly();

    /// <summary>Gets the number of entries in the journal.</summary>
    public int Count => _entries.Count;

    /// <summary>Records a successful engine call.</summary>
    /// <param name="action">The action that was sent to the engine.</param>
    /// <param name="result">What the engine returned, kept so the call log can dump it.</param>
    /// <returns>A new entry added to the journal.</returns>
    public EngineCallJournalEntry RecordSuccess(HoldemAction action, Result<GameData>? result = null)
    {
        var entry = new EngineCallJournalEntry(
            SequenceNumber: _entries.Count + 1,
            Action: action,
            Succeeded: true,
            ErrorMessages: [],
            Result: result);
        _entries.Add(entry);
        return entry;
    }

    /// <summary>Records a failed engine call with error messages.</summary>
    /// <param name="action">The action that was sent to the engine.</param>
    /// <param name="errorMessages">The error messages returned by the engine.</param>
    /// <param name="result">
    /// What the engine returned, kept so the call log can dump it; <c>null</c> when the call threw
    /// and there is no result.
    /// </param>
    /// <returns>A new entry added to the journal.</returns>
    public EngineCallJournalEntry RecordFailure(HoldemAction action, IEnumerable<string> errorMessages, Result<GameData>? result = null)
    {
        var messages = errorMessages.ToArray();
        var entry = new EngineCallJournalEntry(
            SequenceNumber: _entries.Count + 1,
            Action: action,
            Succeeded: false,
            ErrorMessages: messages,
            Result: result);
        _entries.Add(entry);
        return entry;
    }

    /// <summary>Clears all entries from the journal.</summary>
    public void Clear()
    {
        _entries.Clear();
    }
}

/// <summary>
/// A single entry in the engine call journal.
/// </summary>
/// <param name="SequenceNumber">The 1-based sequence number of this call.</param>
/// <param name="Action">The HoldemAction that was sent to the engine.</param>
/// <param name="Succeeded">Whether the engine call succeeded.</param>
/// <param name="ErrorMessages">Error messages if the call failed; empty array if it succeeded.</param>
/// <param name="Result">
/// The <c>var result</c> of the snippet: what the engine returned, or <c>null</c> when the call threw.
/// </param>
public record EngineCallJournalEntry(
    int SequenceNumber,
    HoldemAction Action,
    bool Succeeded,
    string[] ErrorMessages,
    Result<GameData>? Result = null);

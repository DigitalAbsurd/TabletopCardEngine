# Container images

## What this document is

The user guide for the OpenHoldemEngine container images. The decisions behind the host, the
container strategy, the provider model and the security stance are recorded in
[ADR-0005](adr/0005-container-host-and-images.md). Each section below is filled in by the issue
that implements it.

## Security

The API returns `GameData` exactly as the engine does, every seat's hole cards included. It is a
trusted, game-master API: the host is for a trusted network or for running behind a reverse proxy
that authenticates and controls access.

There is no API authentication in the container. Any caller that reaches the container can read
every card and place any action. Protect the container accordingly: restrict inbound access to
trusted hosts only, run it behind an authenticating reverse proxy, or both.

## Configuration

Not implemented yet.

## OpenAPI

Not implemented yet.

## Images

Not implemented yet.

## Testing the images

Not implemented yet.

## Running with compose

Not implemented yet.

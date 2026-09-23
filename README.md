# GatewayCLI

[![CI](https://github.com/OpenChargingCloud/GatewayCLI/actions/workflows/ci.yml/badge.svg)](https://github.com/OpenChargingCloud/GatewayCLI/actions/workflows/ci.yml)

One (OCPP) gateway, with a web interface and a prompt, until 'quit' or Ctrl+C.

A gateway sits between charging stations and whatever they talk to - a CSMS,
a local controller, another gateway - takes WebSocket frames from one side and
hands them on to the other. It is the newest sibling of
[EVCLI](https://github.com/OpenChargingCloud/EVCLI),
[ChargingStationCLI](https://github.com/OpenChargingCloud/ChargingStation) and
the others, and is built the same way: a
[Hermod](https://github.com/Vanaheimr/Hermod) HTTP server carrying a JSON API
and one Server-Sent Events stream, and a web interface built by npm and
embedded into the assembly, so that the gateway is one thing to deploy and
needs nothing installed beside it.

**What is here so far** is what every program of the family has before it does
anything of its own: the sign-in, the name servers, the time servers and the
log. **The forwarding itself is not here yet.** A gateway started today
listens for its web interface, checks its clock and resolves names - and
passes no OCPP frame anywhere.


### Getting it

The libraries it is built from are submodules, so they have to come along:

```
git clone --recurse-submodules https://github.com/OpenChargingCloud/GatewayCLI.git
```

If you already cloned it without them:

```
git submodule update --init --recursive
```

They are fetched from GitHub over https, so nothing but git is needed - no
account, no key.

**On Windows**, turn long paths on first:

```
git config --global core.longpaths true
```

The deepest file in the submodules is 143 characters below the clone root, so
under the classic 260-character limit the root has about 115 characters to
live in. `D:\src\GatewayCLI` is fine; a checkout somewhere below
`C:\Users\<you>\AppData\Local\Temp\...` is not, and the clone fails halfway
through a submodule with `Filename too long` rather than at the start.
Per clone instead of globally: `git clone -c core.longpaths=true ...`.


### Building and running

```
dotnet build GatewayCLI.slnx
dotnet run --project GatewayCLI
```

The build needs the .NET 10 SDK and Node.js. `dotnet build
-p:SkipFrontendBuild=true` leaves the npm step out and reuses whatever is in
`libs/Gateway/Gateway/Frontend/dist`.

The first start makes up one account, `root`, keeps it under `accounts/`
beside the solution and prints its password once. Signing in happens at
Hermod's HTTPExt API, mounted under `/ext`. The web interface is on
<http://127.0.0.1:2353/> - beside the vehicle's 2347, the station's 2348, the
local controller's 2350 and the CSMS's 2351, so that a gateway on the same
bench as the things it sits between fights none of them over a port. `--any`
binds every address instead of the loopback, and `--help` lists the rest.

Who may do what is decided by three roles, each a user group of the HTTPExt
API:

| role          | may                                                        |
|---------------|------------------------------------------------------------|
| `viewer`      | look at the configuration and the log                      |
| `operator`    | that, and run the DNS and NTS tests                        |
| `systemadmin` | that, and change the name servers and the time servers     |

The account made at the first start is a `systemadmin`.


### Name servers and time servers

Both are on the **Configuration** pages, and both are written to
`configuration.json` beside the solution (`--config <file>` puts it
elsewhere). Without the file the gateway runs on the system's name servers and
on four time servers of the PTB, at least two of which must agree; every
change on the pages takes effect at once and is written to the file first.

The **DNS** page looks a name up the way the gateway resolves anything - or
asks one of the name servers on its own. The **NTS** page asks the group of
time servers what the time is, or takes one server apart step by step: the
name, the TLS handshake, the key exchange, the authenticated NTP request. The
clock of the gateway is checked against the group every fifteen minutes. It is
never set from the answer: that is the operating system's business, and a
button that stepped the clock of a running gateway would be a surprise.


### The log

Everything that happens is written three times over, because the three answer
different questions. The **console** shows what is going on to whoever is
watching, at the level `--verbose` and `--quiet` choose. The **Logs** page
keeps the last two thousand entries for whoever asks, and loses them when the
process ends. And `logs/` beside the solution keeps one file per day, every
entry down to the debug ones, for the afternoon somebody asks what happened
last night - `--log-file <dir>` puts it elsewhere, `--no-log-file` leaves it
out, and nothing in it is ever deleted.

What the libraries below write with `DebugX` is picked up too and tagged
`trace`; `--no-trace` leaves it out.


### Typing at it

Once it is up, the console is a prompt rather than a place that only scrolls:

```
gateway:2353> help
```

`help` lists what can be typed, `quit` leaves, **Tab** completes and **↑**
walks back through what was typed before. There are no commands of the
gateway's own yet - a new one is one file in `GatewayCLI/CLI/`, found by
itself.

The log keeps writing while you type, from whichever thread did the thing it is
reporting, and your half-typed line survives it: the line is taken off the
screen, the entry is written whole, and the line comes back with the cursor
where it was.

Where there is no terminal on the input - from a script, under a service
manager, in CI - there is no prompt and nothing to type at, and the gateway
runs until it is stopped.

While working on the web interface, run `npm run watch` in
`libs/Gateway/Gateway/Frontend` and start the gateway with `--frontend
libs/Gateway/Gateway/Frontend/dist`: a reload in the browser then shows the
change, without rebuilding the C# side.


### Where things are

| | |
|---|---|
| `GatewayCLI/` | the command line: switches, and what the console says at a start |
| `GatewayCLI/CLI/` | what can be typed at the running gateway - one file per command |
| `GatewayCLI/PKISetup.cs` | the bench script that built a test PKI; kept for what it knows, not compiled |
| `libs/Gateway/Gateway/` | the gateway itself - its configuration, its log, its JSON API, its web interface |
| `libs/Gateway/Gateway/Frontend/` | the web interface: TypeScript and SCSS, bundled by webpack |
| `libs/Gateway/GatewayTests/` | what the configuration may say, and what it may not |
| `libs/WWCP_OCPP/` | the protocol, and the OCPP gateway the forwarding will be built on |
| `.github/workflows/` | what runs on every push |

The command line is this program's vocabulary and nothing else - the switches
it is started with and the commands it can be typed at. What a gateway *is*,
and what it does, lives in `libs/Gateway`.


### Your participation

This software is Open Source under the **Affero GPL 3.0 license**.
We appreciate your participation in this ongoing project, and your help to
improve it and the e-mobility ICT in general. If you find bugs, want to
request a feature or send us a pull request, feel free to use the normal
GitHub features to do so. For this please read the Contributor License
Agreement carefully and send us a signed copy or use a similar free and
open license.

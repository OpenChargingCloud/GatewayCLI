# GatewayCLI

[![CI](https://github.com/OpenChargingCloud/GatewayCLI/actions/workflows/ci.yml/badge.svg)](https://github.com/OpenChargingCloud/GatewayCLI/actions/workflows/ci.yml)
[![Nightly](https://github.com/OpenChargingCloud/GatewayCLI/actions/workflows/nightly.yml/badge.svg)](https://github.com/OpenChargingCloud/GatewayCLI/actions/workflows/nightly.yml)

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
log - which is [WWCP_Node](https://github.com/OpenChargingCloud/WWCP_Node),
the node the vehicle is built on too; the gateway adds its names, its port,
its roles and its JSON API. **The forwarding itself is not here yet.** A gateway started today
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
API - the gateway's own, handed to the node below, which would otherwise make
a vehicle's:

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
change on the pages takes effect at once and is written to the file first -
and a change to the time servers that the next start would refuse is refused
before anything is written.

An entry of the `dns` section's `servers` is an address or a host name -
`"192.168.1.1"` is one name server, asked over UDP on port 53 - or an object
saying more than that, the form the DNS page writes the list back in:

```json
{ "address": "192.168.1.1", "port": 53, "transport": "UDP", "queryTimeoutSeconds": 2 }
```

`udp://192.168.1.1:53` is how the log names a name server, not a form the file
takes. A file saying it is refused at the start, with the entry named.

The **DNS** page looks a name up the way the gateway resolves anything - or
asks one of the name servers on its own. The **NTS** page is the group of time
servers: a row for each, with what it said in the last synchronisation and the
root CA its last key exchange ended at, an **Edit**, and a **Test** that takes
the server apart step by step - the name, the TLS handshake and every
certificate of the chain with the days it has left, the key exchange, the
authenticated NTP request. Below the servers is what the group is held to, and
last **Sync now**, which asks them all.

The clock of the gateway is checked against the group every fifteen minutes.
It is never set from the answer: that is the operating system's business, and
a button that stepped the clock of a running gateway would be a surprise.
`GET /api/v1/clock` says, to anybody signed in, what time it is here, the group
it is checked against, when it was last checked and how far off it was then.


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
walks back through what was typed before. A command is one file in
`GatewayCLI/CLI/CLICommands/`, found by itself.

`syncNTS` is **Sync now** from the **NTS client** page: the same time servers
asked, the same entries in the log, and afterwards the same result on the page
as its last synchronisation. The one line that differs is the one saying who
asked - the page names the account that pressed the button and tags it `web`,
the prompt says it was the command line and tags it `cli`. Like the button, it
asks and reports and leaves the clock alone. The console gets a line for each
server as well, because the log only records what the group concluded:

```
gateway:2353> syncNTS
succeeded after 852 ms: 4 of 4 server(s) answered (2 required), offset +908.3 ms, spread 5.1 ms
  ptbtime1.ptb.de  +908.4 ms, round trip 41.8 ms, key exchange new
  ptbtime2.ptb.de  +909.6 ms, round trip 41.7 ms, key exchange new
  ptbtime3.ptb.de  +908.2 ms, round trip 41.7 ms, key exchange new
  ptbtime4.ptb.de  +904.6 ms, round trip 53.0 ms, key exchange new
```

With one of the gateway's time servers after it, it is that server's **Test**
button instead: one server, on the ports it is configured with, and every step
with when it happened. Only a server of this gateway is tested; anything else is
answered with the ones there are, and nothing is asked. Tab offers the servers
as soon as the command is typed.

```
gateway:2353> syncNTS ptbtime2.ptb.de
ptbtime2.ptb.de answered, 243 ms altogether:
    +0 ms  Asking ptbtime2.ptb.de: key exchange on port 4460, time on port 123, 10 second(s) allowed.
    +6 ms  'ptbtime2.ptb.de' resolves to 192.53.103.104, 2001:0638:0610:be01:0000:0000:0000:0104.
    +6 ms  Key exchange over TLS ...
  +214 ms  Connected to 192.53.103.104, of 2 address(es) that were offered.
  +214 ms  Where the time went: name 0 ms, TCP 25 ms, TLS 136 ms, key exchange 45 ms.
  +215 ms  TLS 1.3, TLS_AES_128_GCM_SHA256, ALPN ntske/1.
  +217 ms  Server certificate: CN=ptbtime2.ptb.de, for ptbtime2.ptb.de; RSA 3072-bit, sha256RSA; valid 2026-08-09 03:05:52 to 2026-11-07 03:05:51 UTC, 43 day(s) left.
  +218 ms  Intermediate CA: CN=YR1, O=Let's Encrypt, C=US; RSA 2048-bit, sha256RSA; valid 2025-09-03 00:00:00 to 2028-09-02 23:59:59 UTC, 709 day(s) left.
  +218 ms  Intermediate CA: CN=Root YR, O=ISRG, C=US; RSA 4096-bit, sha256RSA; valid 2026-05-13 00:00:00 to 2032-09-02 23:59:59 UTC, 2170 day(s) left.
  +218 ms  Root CA: CN=ISRG Root X1, O=Internet Security Research Group, C=US; RSA 4096-bit, sha256RSA; valid 2015-06-04 11:04:38 to 2035-06-04 11:04:38 UTC, 3175 day(s) left.
  +218 ms  The root's SHA-256 fingerprint: 96bcec06264976f37460779acf28c5a7cfe8a3c0aae11a8ffcee05c0bddf08c6.
  +218 ms  Validated: the chain ends at a root this machine trusts, nothing in it is revoked (asked online), and 'ptbtime2.ptb.de' is one of the server certificate's names.
  +219 ms  The key exchange succeeded: AES_SIV_CMAC_256, 8 cookie(s).
  +219 ms  It named no NTP server of its own, so the time is asked of this host.
  +219 ms  Authenticated NTP request ...
  +243 ms  Answered by 192.53.103.104:123; 8 cookie(s) left, and a fresh one came back.
  +243 ms  Round trip 23.3 ms.
  +243 ms  This gateway's clock is +905.5 ms off what ptbtime2.ptb.de says.
  +243 ms  The clock was not stepped: that is a different thing, with meter readings and certificates hanging off it, and not something a test does by surprise.
```

The log keeps writing while you type, from whichever thread did the thing it is
reporting, and your half-typed line survives it: the line is taken off the
screen, the entry is written whole, and the line comes back with the cursor
where it was.

Where there is no terminal - from a script, under a service manager, in CI, or
with the output going into a file or through `| tee` - there is no prompt and
nothing to type at, and the gateway runs until it is stopped.

While working on the web interface, run `npm run watch` in
`libs/Gateway/Gateway/Frontend` and start the gateway with `--frontend
libs/Gateway/Gateway/Frontend/dist`: a reload in the browser then shows the
change, without rebuilding the C# side.


### Where things are

| | |
|---|---|
| `GatewayCLI/` | the command line: switches, and what the console says at a start |
| `GatewayCLI/CLI/` | the prompt, and in `CLICommands/` what can be typed at it - one file per command |
| `GatewayCLI/PKISetup.cs` | the bench script that built a test PKI; kept for what it knows, not compiled |
| `libs/Gateway/Gateway/` | the gateway itself - what kind of node it is, its roles, its JSON API |
| `libs/Gateway/Gateway/Frontend/` | the web interface: TypeScript and SCSS, bundled by webpack |
| `libs/Gateway/GatewayTests/` | what kind of node a gateway is - its names and its roles - and the event stream |
| `libs/WWCP_Node/` | the node below it, the same as the vehicle's: the log, the configuration file and what it may say, DNS and NTS, the certificate store, the accounts and the web server |
| `libs/WWCP_OCPP/` | the protocol, and the OCPP gateway the forwarding will be built on |
| `.github/workflows/` | what runs on every push, and what runs at night |

The command line is this program's vocabulary and nothing else - the switches
it is started with and the commands it can be typed at. What a gateway *is*,
and what it does, lives in `libs/Gateway`; what every program of the family
is before it is anything in particular lives in `libs/WWCP_Node`.


### Your participation

This software is Open Source under the **Affero GPL 3.0 license**.
We appreciate your participation in this ongoing project, and your help to
improve it and the e-mobility ICT in general. If you find bugs, want to
request a feature or send us a pull request, feel free to use the normal
GitHub features to do so. For this please read the Contributor License
Agreement carefully and send us a signed copy or use a similar free and
open license.

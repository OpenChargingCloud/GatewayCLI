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
anything of its own: the sign-in, the name servers, the time servers, the
certificates they are held to and the log, and the JSON API that serves them -
which is [WWCP_Node](https://github.com/OpenChargingCloud/WWCP_Node), the node
the vehicle is built on too; the gateway adds its names, its port, its roles
and the kinds of certificate it keeps, and adds no route of its own to the
node's JSON API yet. **The forwarding itself is not here yet.** A gateway
started today listens for its web interface, checks its clock and resolves
names - and passes no OCPP frame anywhere.


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

The deepest file in the submodules is 154 characters below the clone root, and
under the classic 260-character limit git creates no file whose whole path is
longer than 259, so the root itself may be at most 104 characters long.
`D:\src\GatewayCLI` is fine; a checkout nested below
`C:\Users\<you>\AppData\Local\Temp\...` can run out of room, and then the
clone fails halfway through a submodule with `Filename too long` rather than
at the start. Per clone instead of globally: `git clone -c core.longpaths=true ...`.


### Building and running

```
dotnet build GatewayCLI.slnx
dotnet run --project GatewayCLI
```

The build needs the .NET 10 SDK and Node.js. The npm step runs only when
something changed under `libs/Gateway/Gateway/Frontend/src`, or under
`libs/WWCP_Node/Frontend/src`, which holds what the web interface of every
kind of node shares and is bundled in as `@node/...`. `dotnet build
-p:SkipFrontendBuild=true` leaves the npm step out and reuses whatever is in
`libs/Gateway/Gateway/Frontend/dist`.

The first start makes up one account, `root`, keeps it under `accounts/`
beside the solution and prints its password once. Signing in happens at
Hermod's HTTPExt API, mounted under `/ext`. The web interface is on
<http://127.0.0.1:2353/> - beside the vehicle's 2347, the station's 2348, the
local controller's 2350 and the CSMS's 2351, so that a gateway on the same
bench as the things it sits between fights none of them over a port. `--any`
binds every address instead of the loopback, and `--help` lists the rest.
They are every node's switches, read by WWCP_Node, as is what the console
says once the gateway is up.

**Recommended for the first start: give `root` your own SSH key** with it, so
that you can type at the gateway over SSH from the start - see
[Typing at it over SSH](#typing-at-it-over-ssh):

```
dotnet run --project GatewayCLI -- --authorize-ssh-key root=C:\Users\you\.ssh\id_ed25519.pub
```

Without it, the first start makes up a key pair for `root` and prints its
private key once, below the password.

Who may do what is decided by three roles when the configuration file says
nothing else, each a user group of the HTTPExt API. What a role may do is an
operation - `read`, `edit` or `run` - on a resource: the node's
`configuration`, `dns`, `nts`, `certificates` and `ssh`; a gateway adds none of
its own yet.

| role          | may                                                                                         |
|---------------|---------------------------------------------------------------------------------------------|
| `viewer`      | read everything: the configuration, the certificates, the SSH server, the log               |
| `operator`    | that, and ask a name server or a time server something                                      |
| `systemadmin` | everything: change the name servers, the time servers, the certificates and the SSH server  |

`viewer` and `systemadmin` are the node's, `operator` is the gateway's, in
`libs/Gateway/Gateway/GatewayAccess.cs`. The account made at the first start is
a `systemadmin`. The configuration file may add roles and say differently what
one of them may do, in a `roles` section - see
[WWCP_Node's README](https://github.com/OpenChargingCloud/WWCP_Node#who-may-sign-in).


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

A server can be held to more than a certificate authority vouching for it. In
a server's dialog on the **NTS** page - and on the **DNS** page for a name
server reached over TLS or HTTPS, the only ones that show a certificate - go
the fingerprints of the certificates it may show and of the roots its chain may
end at, the one it showed last and the ones the certificate store keeps for it
offered with a click; what a mismatch comes to, refused, recorded or accepted;
and whether it is held to what it is first believed with. A chain has to end at
a root the machine it runs on trusts, or at one of the gateway's own - a TLS
root of its store kept for that kind of server, or any root of the store the
server is held to (see [Certificates](#certificates)); a pin then narrows that
to the certificates and roots it names. What every server was last believed
with is kept in `known-servers.json` beside the configuration file -
fingerprints and nothing else - so that another certificate is noticed even
where a server is held to none.

What is learned on first use is written into the server's entry at the first
key exchange or handshake after a save, mostly with the NTS or DNS page still
open. So the pages send every server back with what they showed it held to,
under `pinsAsShown`: their next save keeps what was learned in between, and
still takes away a pin that was shown and removed there.

The clock of the gateway is checked against the group every fifteen minutes.
It is never set from the answer: that is the operating system's business, and
a button that stepped the clock of a running gateway would be a surprise.
`GET /api/v1/clock` says, to anybody signed in, what time it is here, the group
it is checked against, when it was last checked and how far off it was then.


### Certificates

Everything this gateway believes, the certificate it presents and every server
it recognises lives in one store, `certificates/` beside the configuration
file - so beside the solution unless `--config` says otherwise - and is managed
on the **Certificates** and **Identities** pages or from the command line. A
gateway keeps the four kinds of TLS and none of the seven of ISO 15118, which
are a vehicle's.

The store is the directory: one file per certificate and kind below it, and an
`index.json` recording what a file cannot say about itself: what somebody calls
it, whether it is switched on and what it is kept for. So a store copied to
another machine arrives complete, and a lost index costs labels, switches and
usages rather than certificates.

A **tlsRoot** says which time server and which name server over TLS or HTTPS
may be believed, beside the roots of the machine the gateway runs on - and is
told what it is for, `nts`, `dns` or both, because a root kept for the name
servers alone vouches for no time. A **tlsServer** is a server's own
certificate, kept so that the server can be held to it by its fingerprint; a
server's dialog on the **NTS client** and **DNS client** pages offers the ones
the store keeps for it. A **clientRoot** - what a client connecting to the
gateway will have to chain to, a root or the CA below one that issues the
clients - and a **tlsIdentity** - who the gateway is as a client, with its
private key: what it will show a server that asks - are kept, and used by
nothing here yet, so a page offers them no uses; a client's identity is shown
on no listener. A gateway keeps no **tlsServerIdentity**, who a server of a
node is, offered the listeners it is shown on: it names none, and has no
**Server identities** page. Any certificate may still be marked with a usage
made up - a mark nothing here acts on until a configuration or code names it.

One certificate may be kept as several kinds - a self-signed identity as the
root its peers are judged against, say - each switched on and off and told its
usages on its own, under one handle and one name; switched off or deleted
without a kind, it goes as every kind. A kind may be made up at the upload as
well, kept below `certificates/custom/`.

**Certificates** keeps certificates alone, with no private key: the roots and
the server certificates - an upload there leaves a key in the box out, and
says where it goes. **Identities** keeps who the gateway is as a client, each
with its key. Each page has three tabs: by usage, every certificate once with
every kind it is kept as, and the upload, where certificates are pasted or
files dropped, what is in them is said certificate by certificate before
anything goes in, and every one of them is kept as every kind ticked.

```
dotnet run --project GatewayCLI -- \
    --import-certificate tlsRoot=our-time-servers-root.pem \
    --list-certificates
```

Importing a root makes it believed - for every use, until the Certificates page
says what it is for. `--list-certificates` prints every certificate with its
handle, and `--certificates <dir>` points the gateway at another store.

PEM, DER and PKCS#12 all go in. A root is a certificate on its own; a
tlsIdentity has to bring its private key, so a PEM for one holds the key
beside the certificate and the sub-CAs above it - the file `openssl` writes
when it is given all three. An encrypted key block is opened with the same
password a protected PKCS#12 would be. Certificates already in the store
directory - copied in by hand, restored from a backup - are read again at every
start and adopted.

Switching a certificate off is not the same as deleting it: the first leaves
the file where it is, for the afternoon somebody takes a root out of service;
the second deletes it, because a store whose "delete" left the private key on
the disk would be worse than one with no delete at all. Time switches a
certificate off as well, and separately - an expired certificate stays listed
and stops being used. Only a `systemadmin` changes the store; anybody signed in
may look at it.

**The private keys in the store are not encrypted.** A PKCS#12 is opened with
its password once, at import, and written back without one. What guards them
is the file system: the store directory is made for its owner alone where the
platform allows saying so in one call, which on Windows means the ACL a new
directory inherits and nothing more. Anybody who can read `certificates/` can
take this gateway's identity, so it belongs on a machine whose users are all
trusted with exactly that. The gateway says so at every start, and at every
import of a key.

A password for an import is read from `GATEWAY_CERT_PASSWORD` where
`--certificate-password` is not given. A password given as a switch stands in
the process list for every other user of the machine.


### The log

Everything that happens is written three times over, because the three answer
different questions. The **console** shows what is going on to whoever is
watching, at the level `--verbose` and `--quiet` choose. The **Logs** page
keeps the last two thousand entries for whoever asks, and loses them when the
process ends. And `logs/` beside the solution keeps one file per day, every
entry down to the debug ones, for the afternoon somebody asks what happened
last night - `--log-file <dir>` puts it elsewhere, `--no-log-file` leaves it
out, and nothing in it is ever deleted. Which directory it is, the start says
under `log files`, and the Configuration page on its Event log card.

Beside the files, `logs/metrological/` is the **log book**, and the one of
them that is evidence rather than a record: what bears on the time the gateway
stamps things with - its starts and its ends, the plan of its clock check,
every synchronisation with what each server answered, what was news about a
time server's certificate, every change of the time servers. One JSON object
per line and one file per day, each line carrying the hash of the one before
it and signed with an ECDSA P-256 key kept beside it, `signing-key.pem`, whose
public half is `signing-key.pub.pem`. Nothing in it is thinned out, and
without log files there is none. It is
[WWCP_Node](https://github.com/OpenChargingCloud/WWCP_Node)'s, and its README
says what is written there and how a chain is checked.

A change of the DNS or NTS settings or of the certificate store that comes in
over the JSON API - from a page or from a script - is a `notice` line naming
the account behind it: `'admin' changed the time source of this gateway.`

What the libraries below write with `DebugX` is picked up too and tagged
`trace`; `--no-trace` leaves it out.


### Typing at it

Once it is up, the console is a prompt rather than a place that only scrolls:

```
gateway:2353> help
```

`help` lists what can be typed, `quit` leaves, **Tab** completes and **↑**
walks back through what was typed before. The commands every node has -
`syncNTS` among them - come with WWCP_Node's `NodeCLI`, which the prompt is
built on; a command of the gateway's own would be one file beside
`GatewayCLI/CLI/GatewayCLI.cs`, found by itself. There is none yet.

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
nothing to type at, and the gateway runs until it is stopped: by Ctrl+C, or
by the SIGTERM a service manager sends.

While working on the web interface, run `npm run watch` in
`libs/Gateway/Gateway/Frontend` and start the gateway with `--frontend
libs/Gateway/Gateway/Frontend/dist`: a reload in the browser then shows the
change, without rebuilding the C# side.

The web interface's own test, `src/pages/pages.test.ts` in the same directory,
holds the gateway's pages to what every page of every node is held to, by
WWCP_Node's `Frontend/test/pages.ts`: a page with a form says whether it holds
a draft, holds every form it has, asks before its Reload throws one away, and
reads a number so that an emptied field is not 0. None of the gateway's pages
has a form yet, and the test says so - a page that gets one turns it red until
it is named there. `npm run typecheck:test` and `npm test` run it, as the CI
does; what the pages of every node share is tested in WWCP_Node.


### Typing at it over SSH

The same prompt is served over SSH, on port 22353 — twenty thousand above the
web interface's — and on the addresses the web interface listens on: the
loopback, or every address with `--any`. Nothing else is: no shell of the
machine, no files, no tunnels. `--ssh-port` moves it, `--no-ssh` switches it
off.

The **SSH server** page under Configuration shows whether it runs, where, and
whether passwords open it; its host key, with the fingerprint and a
`known_hosts` line; who is at the command line now; the keys every account
may sign in with; its limits and its algorithms. A `systemadmin` switches it on
and off, moves it to another port or lets passwords open it there, at once and
saved in the `ssh` section of the configuration file - a port that is taken is
refused, and the server stays where it was. `--no-ssh` and `--ssh-port` still
win over the file, and the page says so.

Whoever signs in is an account of the gateway, under its name, with a key of
its own. The first start makes `root`; give it your public key with that very
start - the way recommended:

```
dotnet run --project GatewayCLI -- --authorize-ssh-key root=C:\Users\you\.ssh\id_ed25519.pub
```

The private key then stays on your machine, and no console ever shows it.
A first start without `--authorize-ssh-key root=...` makes up a key pair for
`root` instead and prints its private key once, right below the first-start
box with the password: save the lines from `-----BEGIN OPENSSH PRIVATE KEY-----` to
the END line as a file only you can read, and sign in with
`ssh -i <file> -p 22353 root@127.0.0.1`, or import the file in PuTTYgen for
PuTTY. Like the password it is kept nowhere - but a console may be kept, by a
service's journal or a redirected output; replace it with your own key and
take it out. `--authorize-ssh-key` also works at any later start.

An OpenSSH `.pub` goes in as it is, and so does what PuTTYgen saves with *Save
public key*. The key is kept with the account, beside its password. Then:

```
ssh -p 22353 root@127.0.0.1
```

or, in PuTTY, host `127.0.0.1`, port `22353`, *Connection → Data → Auto-login
username* `root`, and the private key under *Connection → SSH → Auth →
Credentials*. The first time, PuTTY asks whether to trust the gateway's host
key: the banner prints its fingerprint under `SSH`, and the SSH server page
shows it, to compare it with.

The `sshKeys` command manages an account's keys - at the console, or over SSH
for the account signed in:

```
sshKeys root
sshKeys root add ssh-ed25519 AAAA... you@laptop
sshKeys root remove SHA256:abc
```

`remove` takes the fingerprint `sshKeys` lists, or enough of its beginning, and
locks that key out at once. `apiKeys` does the same for the account's API keys,
and shows a new one once, when it is made.

Everything works as at the console — Tab, the history, the log above the line
being typed — with three differences. `quit`, `exit` and Ctrl+D leave the
session, and the gateway keeps running. The account may do what its roles let
it do on the web interface, and the log names it: "'root' at the command line
over SSH asked this gateway to synchronise its time.", tagged `cli` and `ssh`.
And the session's log starts at the console's level and is its own: `log debug`
shows everything here, `log off` nothing, for this session alone. `who` says
who else is signed in.


### Where things are

| | |
|---|---|
| `GatewayCLI/` | the program: every node's switches and banner - WWCP_Node's, in a gateway's words - and the gateway it starts with them |
| `GatewayCLI/CLI/` | the prompt, on WWCP_Node's `NodeCLI`, which brings what every node can be typed at - and a command of the gateway's own, one file each, once there is one |
| `GatewayCLI/PKISetup.cs` | the bench script that built a test PKI; kept for what it knows, not compiled |
| `libs/Gateway/Gateway/` | the gateway itself - what kind of node it is, its roles, the kinds of certificate it keeps, and its JSON API, the node's with nothing of its own on top yet |
| `libs/Gateway/Gateway/Frontend/` | the web interface: TypeScript and SCSS, bundled by webpack together with what every node's pages stand on, imported as `@node/...` |
| `libs/Gateway/GatewayTests/` | what kind of node a gateway is - its names, its roles, which of its kinds of certificate is on which page and offered what it may be for - and the node's conformance suite, asked of a gateway |
| `libs/WWCP_Node/` | the node below it, the same as the vehicle's: the log, the configuration file and what it may say, DNS and NTS, the certificate store, the accounts, the web server and the JSON API every node answers, in `Frontend/src` what every node's web interface shares - and in `WWCP_Node_TestKit/` what every node has to pass |
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

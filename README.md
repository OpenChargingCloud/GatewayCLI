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
the vehicle is built on too. On top of that a gateway is an OCPP node as a
charging station and a local controller are: it **dials its CSMS** - one or
several - over the connections written down on its **Connections** page, with
what they sign in with, and keeps them up, as both of them do, because that is
the same code (`OCPPDiallingNode` in
[WWCP_OCPP](https://github.com/OpenChargingCloud/WWCP_OCPP)'s
`WWCP_OCPP_Node`). And it **is dialled**, as a local controller is: by the
charging stations and local controllers below it, and by a CSMS it cannot dial,
on its **station server** - the same server, logins, pages and section of the
configuration file as a local controller's (`OCPPServingNode`, from the same
place). **Not here yet:** **the forwarding itself** - a gateway started today
lets in what is below it and dials its CSMS, and passes no OCPP message on to
anybody.


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

The build needs the .NET 11 SDK and Node.js. The npm step runs only when
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
binds every address instead of the loopback, and `--help` lists the rest -
among them HTTPS's `--https`, `--https-cert <file>`, `--https-key <file>`,
`--redirect-port <number>` and `--trusted-proxy <address>`. They are every
node's switches, read by WWCP_Node, as is what the console says once the
gateway is up.

**`--any` puts the sign-in on the network.** With it come the password, the
API keys and the session cookie - in the clear, unless the gateway speaks
HTTPS itself or a proxy in front of it does and is named as trusted, both in
the `web` section of the configuration file. Behind nginx:

```json
"web": { "trustedProxies": [ "127.0.0.1", "::1" ] }
```

With certbot's files and no proxy:

```json
"web": { "tls": { "certificate": { "fullchain": "/etc/letsencrypt/live/gateway.example.org/fullchain.pem",
                                   "privateKey": "/etc/letsencrypt/live/gateway.example.org/privkey.pem" },
                  "redirectPort": 80 } }
```

or `"certificate": "store"` with the store's TLS server identity, put there
with `--import-certificate tlsServerIdentity=<file>` or on the **Server
identities** page. The switches say the same and win over the file:

```
dotnet run --project GatewayCLI -- --any --https --import-certificate tlsServerIdentity=gateway.p12
```

Then the session cookie is a secure one and HSTS is sent; behind a trusted
proxy that is decided per request, from its `X-Forwarded-Proto`, and the
client it names is who the log and the sign-in's ration of guesses see.
Neither said, with `--any`, the gateway warns in its log, below its banner
and above every page of its web interface to its administrators.
[WWCP_Node's README](https://github.com/OpenChargingCloud/WWCP_Node#https-and-the-proxies-in-front)
says the rest.

**Recommended for the first start: give `root` your own SSH key** with it, so
that you can type at the gateway over SSH from the start - see
[Typing at it over SSH](#typing-at-it-over-ssh):

```
dotnet run --project GatewayCLI -- --authorize-ssh-key root=C:\Users\you\.ssh\id_ed25519.pub
```

Without it, the first start makes up a key pair for `root` and prints its
private key once, below the password.

Who may do what is decided by four roles when the configuration file says
nothing else, each a user group of the HTTPExt API. What a role may do is an
operation - `read`, `edit` or `run` - on a resource: the node's
`configuration`, `dns`, `nts`, `certificates`, `ssh`, `log` and the rest of
WWCP_Node's, and the gateway's `connections` - where it dials and what it signs
in with there, under the name a charging station gives them - and `stations` -
its station server and the stations that may sign in to it, under the name a
local controller gives them.

| role          | may                                                                                                                    |
|---------------|------------------------------------------------------------------------------------------------------------------------|
| `viewer`      | read everything: the configuration, the connections, the stations, the certificates, the SSH server, the log           |
| `support`     | see what is going on and restart an idle node, and nothing else                                                        |
| `operator`    | read everything, ask a name server or a time server something, and test a connection                                   |
| `systemadmin` | everything: change the name servers, the time servers, the connections, the stations, the certificates and the SSH server |

`viewer`, `support` and `systemadmin` are the node's, `operator` is the gateway's, in
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
takes. A file saying it is refused at the start, with the entry named. A name
server has an id as a time server has, `{ "id": "home", "address":
"192.168.1.1" }`, or `dns-<n>` by its place, and a save of `"servers": null`
gives the gateway back the name servers it was started with - the DNS page has
a button for it.

The time servers are named in groups, under the `nts` section's `groups`: the
legal one, which legal time rests on - the PTB's four, of which two have to
answer, while it names none - and as many others as a gateway is given, each
switched off until it names a server:

```json
"nts": { "groups": {
  "legal": { "servers": [ "ptbtime1.ptb.de", "ptbtime2.ptb.de" ], "minServers": 2 },
  "local": { "servers": [ "lc1.local" ], "checkEverySeconds": 30 }
} }
```

A section that says anything outside of `groups` - `"nts": { "enabled": false }`,
as files said before there were groups - is refused at the start, with where
its keys belong: `"nts": { "groups": { "legal": { "enabled": false } } }`.

Every time server has an id that names it across all groups: one of its own,
`{ "id": "lc-time", "hostname": "lc1.local" }` - lower-case letters, digits and
hyphens, at most 50 characters - or, without one, `<group>-<n>` by its place,
`local-1`. A server keeps its id when another is deleted or its name changes,
and the log says of every change of a group, as of the name resolution, who
made it, `(by web:root)`.

The **DNS** page looks a name up the way the gateway resolves anything - or
asks one of the name servers on its own. The **NTS** page has a tab for each
group of time servers, the legal one first, and a **+** tab that makes another,
switched off. A group's tab has a row for each of its servers, with what it
said in the last synchronisation and the root CA its last key exchange ended
at, an **Edit**, and a **Test** that takes the server apart step by step - the
name, the TLS handshake and every certificate of the chain with the days it
has left, the key exchange, the authenticated NTP request. Below the servers is
what the group is held to - what counts as legal time on the legal group's tab
alone - and last **Sync now**, which asks them all. A group switched off can be
taken away there.

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
to the certificates and roots it names. A server held to a group of roots,
`"rootGroup": "csms1"` in its entry, is anchored at the roots of the store
marked `ntsca-csms1` - `dnsca-csms1` for a name server - and at no other: not
at one the machine trusts, and not at a pinned one outside the group. What
every server was last believed with is kept in `known-servers.json` beside the
configuration file - fingerprints and nothing else - so that another certificate is noticed even
where a server is held to none.

What is learned on first use is written into the server's entry at the first
key exchange or handshake after a save, mostly with the NTS or DNS page still
open. So the pages send every server back with what they showed it held to,
under `pinsAsShown`: their next save keeps what was learned in between, and
still takes away a pin that was shown and removed there.

The clock of the gateway is checked against every group switched on - the legal
one every fifteen minutes unless its `checkEverySeconds` says otherwise.
It is never set from the answer: that is the operating system's business, and
a button that stepped the clock of a running gateway would be a surprise.
`GET /api/v1/clock` says, to anybody signed in, what time it is here, the groups
it is checked against, when it was last checked and how far off it was then.


### Connections

Where a gateway dials is written down on the **Connections** page, as on a
charging station and a local controller - the same page, the same store and the
same JSON API (`/api/v1/configuration/connections`), from WWCP_OCPP's
`WWCP_OCPP_Node`. A connection is a `ws://`, `wss://` or `ssh://` address of a
CSMS, what it signs in with - a password or the one-time password of a TOTP
secret, written down on the **Credentials** tab of the same page - and, over
TLS, the client identity it shows and how its server is believed: by a root of
the certificate store kept for `ocpp`, or by the key pins of the server's key.
Several connections are several CSMS, each dialled on its own; one that dials
by itself is dialled at the start and from the moment it is written down, and
dialled again when it is lost. The keys a connection signs in with over SSH
stand among the client identities, on their **New key** tab.

The store is `ocpp-connections/` beside the configuration file. Who the gateway
is in OCPP - `gw001` unless said otherwise - is the `ocpp` section of the
configuration file, with the fields a local controller's has:

```json
"ocpp": { "nodeId": "gw001", "vendorName": "GraphDefined GmbH", "model": "Gateway" }
```

Everybody signed in may look at the connections, an `operator` may test one,
and only a `systemadmin` changes them.


### Charging stations

What is below a gateway - charging stations, local controllers - connects to
its **station server**, which is a local controller's: the same section of the
configuration file, the same pages and the same JSON API
(`/api/v1/configuration/ocpp-server`), from WWCP_OCPP's `WWCP_OCPP_Node`
(`OCPPServingNode`).

- **Charging stations** switches the server on and says how it listens: the
  `ocppServer` section - address, port, the security profiles and OCPP versions
  it takes, the names it is reachable as, TLS, connection limits, pings,
  logging, and OCPP over the gateway's SSH server.
- **Logins and groups** are who may sign in, under the name its address ends
  with: by password, one-time password, SSH key, client certificate or the key
  pin of one, under the rules of its login group.
- **Server certificates** are the keys and certificates the port presents -
  made here with their signing request, or a key of its own that stations pin.
- **Accepted chains** are what a station's own certificate has to lead to.

Off unless switched on, and at port **2354** where the section names none -
beside the web interface on 2353, and not on a local controller's 2351:

```json
"ocppServer": { "enabled": true, "port": 2354, "securityProfiles": [ 2, 3 ], "reachableAs": [ "gateway.example.org" ] }
```

`"http3": true` takes WebSockets over HTTP/3 as well, on the UDP port of the
same number - 2354/udp here, which a firewall has to let through as well. Off
where the file says nothing, only on a port that is encrypted, and switched on
and off at once on the **Charging stations** page. The section is the one a
charging station has; a gateway has one port for every version of OCPP, offers
OCPP 1.6 where `subprotocols` names `"ocpp1.6"`, and refuses `port16`, which is
a station's.

A CSMS the gateway cannot dial dials it on the same port: written down on the
**Connections** page as a connection the other end makes, it is let in under
the name its address ends with by the credentials it names, and is a
connection of the gateway from then on - not a station of it.

The CSMS reads and sets the station port over OCPP as the page does: the
component `OCPPServerController` of the gateway's own Device Model, with
GetVariables and SetVariables addressed to the gateway itself. What waits for a
restart is written down and answered `RebootRequired`; switching the port off
waits until the answer is out. The stations that may sign in and their login
groups are `StationLoginController` and `StationLoginGroupController`, as the
page lists them: a station is listed with its password, key or pins, and read
back without any secret. Who may is the resource `stations`, once the
gateway's signature policy is switched on. A station on the port cannot ask the
gateway what only its CSMS asks, nor send another station a request: what a
station sends on goes up to the CSMS, in the station's own name.

The stores are `ocpp-stations.json`, `ocpp-client-trust/` and
`ocpp-server-keys/` beside the configuration file. Everybody signed in may
look at them; only a `systemadmin` changes them.


### Certificates

Everything this gateway believes, the certificate it presents and every server
it recognises lives in one store, `certificates/` beside the configuration
file - so beside the solution unless `--config` says otherwise - and is managed
on the **Certificates**, **Identities** and **Server identities** pages or from
the command line. A gateway keeps the four kinds of TLS, the identity its web
interface shows over HTTPS, and none of the seven of ISO 15118, which are a
vehicle's.

The store is the directory: one file per certificate and kind below it, and an
`index.json` recording what a file cannot say about itself: what somebody calls
it, whether it is switched on and what it is kept for. So a store copied to
another machine arrives complete, and a lost index costs labels, switches and
usages rather than certificates.

A **tlsRoot** says which time server, which name server over TLS or HTTPS and
which CSMS of a `wss://` connection may be believed, beside the roots of the
machine the gateway runs on - and is told what it is for, `nts`, `dns`, `ocpp`
or several, because a root kept for the name servers alone vouches for no time.
A **tlsServer** is a server's own certificate, kept so that the server can be
held to it by its fingerprint; a server's dialog on the **NTS client** and
**DNS client** pages offers the ones the store keeps for it. A **tlsIdentity**
is who the gateway is as a client, with its private key: what it shows a CSMS
that asks - the **Connections** page says which one each connection shows. It
is brought with its key, or its key is made on the **Client identities** page
and only its signing request and the certificate that comes back travel. A
**clientRoot** - what a client connecting to the gateway will have to chain to,
a root or the CA below one that issues the clients - is kept, and used by
nothing here yet. A **tlsServerIdentity**, with its private key, is what the web
interface, the JSON API and the sign-in show over HTTPS, where the `web`
section says `"tls": { "certificate": "store" }` or the command line `--https`,
and is looked after on the **Server identities** page. Any certificate may
still be marked with a usage made up - a mark nothing here acts on until a
configuration or code names it.

One certificate may be kept as several kinds - a self-signed identity as the
root its peers are judged against, say - each switched on and off and told its
usages on its own, under one handle and one name; switched off or deleted
without a kind, it goes as every kind. A kind may be made up at the upload as
well, kept below `certificates/custom/`.

**Certificates** keeps certificates alone, with no private key: the roots and
the server certificates - an upload there leaves a key in the box out, and
says where it goes. **Identities** keeps who the gateway is as a client, each
with its key, and **Server identities** what its web interface shows over
HTTPS. Each page has three tabs: by usage, every certificate once with
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
`libs/Gateway/Gateway/CommandLine/GatewayCLI.cs`, found by itself. There is
none yet. The command line is the gateway's and not this program's: whoever
signs in over SSH gets it whichever program runs the gateway, and
`libs/Gateway/GatewayTests/` tests it.

`syncNTS` is **Sync now** from the legal group's tab of the **NTS client**
page, and `syncNTS <group>` from another's: the same time servers asked, the
same entries in the log, and afterwards the same result on the page as its last
synchronisation. `listNTSGroups` lists the groups, each with its servers and
what its last check found. The one line that differs is the one saying who
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
loopback, or every address with `--any`. Nothing else is - no shell of the
machine, no tunnels -, and files only where SFTP is switched on (see
[Files over SFTP](#files-over-sftp)). `--ssh-port` moves it, `--no-ssh`
switches it off.

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
locks that key out at once, and ends an SSH session it let in. `apiKeys` does
the same for the account's API keys; `apiKeys root add key <key>` takes a key
of your own - at least 20 letters, digits or `. _ ~ -` - and without one it
makes one. An API key is shown once, when it is made: the gateway keeps its
SHA-256 alone, and lists and logs it by its first six characters and the
beginning of its hash, `alices... #41WyMKor`; `remove` takes the key, that
beginning of it, or `#` and the beginning of its hash. A key is read-only
unless `readWrite` says otherwise, and a read-only key changes nothing - not
below `/ext` either, where the account, its keys and its password are: 403.

Every account looks after its own keys on the web interface too: the name at
the foot of the menu leads to its page, with its details, its API keys and its
SSH keys - a key made or added there, switched off and on again, or removed,
each said in the log, tagged `web`, an API key by its hint and hash. A key
switched off lets nobody in until it is switched on again, and `sshKeys` lists
it with ", switched off".

Everything works as at the console — Tab, the history (kept per account in
`accounts/history/` and brought back at the next session, without a line that
names an API key), the log above the line
being typed — with three differences. `quit`, `exit` and Ctrl+D leave the
session, and the gateway keeps running. The account may do what its roles let
it do on the web interface, and the log names it: "'root' at the command line
over SSH asked this gateway to synchronise its time.", tagged `cli` and `ssh`.
And the session's log starts at the console's level and is its own: `log debug`
shows everything here, `log off` nothing, for this session alone. `who` says
who else is signed in.


### Files over SFTP

The gateway also serves its log files over SFTP, on the same port and to the
same keys, where the `ssh` section of the configuration file says so:

```json
"ssh": { "sftp": true }
```

It is off unless it says `true`, and a `systemadmin` switches it on the SSH
server page as well - without a restart: the next session is served, and
switching it off ends the SFTP sessions that are open. The log line about the
SSH server at the start says whether SFTP is served.

```
sftp -P 22353 root@127.0.0.1
```

or WinSCP with the same host, port, user and key. The root shows `/logs`, with
the day's log files, and `/logs/metrological`, with the metrological log and
`signing-key.pub.pem` - never the private signing key. Whoever may read the
Logs page reads them, and nobody writes there, `root` included: no upload, no
folder, nothing removed or renamed. The log says who opened SFTP from where,
every file downloaded, and in one line what the session came to; `who` and
the SSH server page list an SFTP session beside the command lines.

The `ssh` section's `accessRules` narrow who comes in, and never let in
anyone the accounts, roles and keys would not: an ordered list, the first
rule that matches deciding, each with an `action` - `allow` or `deny` - and
what it is about, left out for everything: `principal` (`account:root`,
`group:operator`, with `*` and `?`), `service` (`shell`, `sftp`), `source`
and `localAddress` (addresses or networks), `authMethod` and a `comment`.

```json
"ssh": { "sftp": true, "accessRules": [
  { "action": "deny", "service": "sftp", "source": [ "10.0.0.0/8" ], "comment": "no files from the field" }
] }
```

A rule misspelt is refused at the start rather than ignored, a session the
rules now refuse ends, and the log says which rule refused whom, from where.


### Where things are

| | |
|---|---|
| `GatewayCLI/` | the program: every node's switches and banner - WWCP_Node's, in a gateway's words - and the gateway it starts with them |
| `GatewayCLI/PKISetup.cs` | the bench script that built a test PKI; kept for what it knows, not compiled |
| `libs/Gateway/Gateway/` | the gateway itself - what kind of node it is, its roles, the kinds of certificate it keeps, who it is in OCPP and the OCPP node it dials with (`Gateway.Dialling.cs`), its station server (`Gateway.Serving.cs`), and its JSON API: the node's and that of every node that dials and serves, with nothing of its own on top yet |
| `libs/Gateway/Gateway/CommandLine/` | the prompt, on WWCP_Node's `NodeCLI`, which brings what every node can be typed at - and a command of the gateway's own, one file each, once there is one |
| `libs/Gateway/Gateway/Frontend/` | the web interface: TypeScript and SCSS, bundled by webpack together with what every node's pages stand on, imported as `@node/...`, and the pages of the connections and of the station server every node that dials and serves shares, imported as `@ocpp/...` - whose tests run here as well |
| `libs/Gateway/GatewayTests/` | what kind of node a gateway is - its names, its roles, which of its kinds of certificate is on which page and offered what it may be for, its web interface over HTTPS and behind a trusted proxy, its command line, that it dials its CSMS and hangs up, that what is below it and a CSMS that dials are let in on its station server - and the node's conformance suite, asked of a gateway |
| `libs/WWCP_Node/` | the node below it, the same as the vehicle's: the log, the configuration file and what it may say, DNS and NTS, the certificate store, the accounts, the web server and the JSON API every node answers, in `Frontend/src` what every node's web interface shares - and in `WWCP_Node_TestKit/` what every node has to pass |
| `libs/WWCP_OCPP/` | the protocol, the OCPP gateway node the gateway speaks through and the forwarding will be built on, and in `WWCP_OCPP_Node/` what every node that dials a CSMS shares with a charging station and a local controller - its connections, how they are dialled, and their pages - and what a node that is dialled by the stations below it shares with a local controller: its station server, its stores and its pages |
| `.github/workflows/` | what runs on every push, and what runs at night |

This program is the switches it is started with and nothing else. What a
gateway *is*, what it does and what it can be told at its prompt lives in
`libs/Gateway`; what every program of the family is before it is anything in
particular lives in `libs/WWCP_Node`.


### Your participation

This software is Open Source under the **Affero GPL 3.0 license**.
We appreciate your participation in this ongoing project, and your help to
improve it and the e-mobility ICT in general. If you find bugs, want to
request a feature or send us a pull request, feel free to use the normal
GitHub features to do so. For this please read the Contributor License
Agreement carefully and send us a signed copy or use a similar free and
open license.

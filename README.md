# vccs-compile

A compiler from **Value-Passing CCS** to **Pure CCS**, written in F# with a hand-built lexer/parser pipeline.

> Lets you write concise process algebra specifications with typed integer data, then automatically expands them into the pure synchronisation model understood by formal verification tools like [CAAL](http://caal.cs.aau.dk).

---

## What it does

[CCS (Calculus of Communicating Systems)](https://en.wikipedia.org/wiki/Calculus_of_communicating_systems) is a formal language for modelling concurrent processes. The value-passing extension lets processes send and receive integer data over channels. vccs-compile takes a `.vccs` source file and compiles it to `.ccs` by expanding each typed channel into a family of distinct channels, one per possible value. Conditional guards are evaluated statically and dead branches are eliminated.

**Input** (`counter.vccs`):

```text
Counter(x:[0,3]) =
  'read(x).Counter(x)
  + (if x < 3 then 'inc(x).Counter(x+1))
  + (if x > 0 then 'dec(x).Counter(x-1));
```

**Output** (`counter.ccs`):

```text
Counter_0 = (('read_0.Counter_0 + 'inc_0.Counter_1) + 0)
Counter_1 = (('read_1.Counter_1 + 'inc_1.Counter_2) + 'dec_1.Counter_0)
Counter_2 = (('read_2.Counter_2 + 'inc_2.Counter_3) + 'dec_2.Counter_1)
Counter_3 = (('read_3.Counter_3 + 0) + 'dec_3.Counter_2)
```

Four concrete states are emitted from one parameterised definition. The `if x < 3` guard at `x=3` and `if x > 0` guard at `x=0` evaluate to false and disappear.

---

## Features

- **Typed intervals** — parameters declared as `x:[L,H]` enumerate every value in range
- **Arithmetic expressions** — `x+1`, `x-1`, `x*2` usable in output actions and recursive calls
- **Static guard elimination** — `if/then/else` conditions evaluated at compile time; unreachable branches not emitted
- **Full CCS operator set** — action prefixing, choice (`+`), parallel (`|`), restriction (`\`), renaming (`[]`)
- **Interactive REPL** — incrementally define and compile processes, with undefined-reference warnings
- **CAAL export** — `--caal` flag produces syntax directly loadable into the [CAAL](http://caal.cs.aau.dk) verification tool

---

## Getting started

**Prerequisite:** [.NET 10 SDK](https://dotnet.microsoft.com/download)

```bash
# Compile a file (outputs test/full.ccs)
dotnet run -- test/full.vccs

# Compile for CAAL (semicolons, tau keyword, channel-only rename syntax)
dotnet run -- test/full.vccs --caal

# Interactive REPL
dotnet run
```

**REPL commands:** `#list`, `#clear`, `#quit`

---

## Language syntax

```text
// Input action with typed variable
Buf = recv(x:[0,3]).BufFull(x);
BufFull(x:[0,3]) = 'send(x).Buf;

// Conditional guards
Sem(s:[0,1]) =
  (if s == 0 then acquire(a:[1,1]).Sem(1))
  + (if s == 1 then release(r:[1,1]).Sem(0));

// Parallel composition and restriction
ReliableChannel = (Sender | Receiver) \ {medium:[0,2]};

// Channel renaming
ChocVM = VM [item/choc:[1,1]];
```

Full examples including a 2-place FIFO buffer, binary semaphore, coffee machine, and reliable channel are in [`test/full.vccs`](test/full.vccs).

---

## Architecture

```text
.vccs source
    |
    +-- Lexer  (FsLex)    tokenisation
    +-- Parser (FsYacc)   AST construction  ->  Vccs.P
    +-- Compiler          value enumeration, guard evaluation  ->  Pccs.P
    +-- Printer           emit .ccs (default) or CAAL-compatible format
```

For a full walkthrough of the design and encoding rules, see the [project presentation](docs/Software/build/project-presentation.pdf).

The compiler works in a single pass. `getValuations` enumerates the Cartesian product of all parameter domains; `compileP` then recursively substitutes concrete values and evaluates guards, producing one flat `Pccs` definition per valuation.

---

## Tech stack

| | |
|---|---|
| Language | F# on .NET 10 |
| Lexer | FsLex (regex-based scanner) |
| Parser | FsYacc (LALR(1) grammar) |
| Verification target | [CAAL](http://caal.cs.aau.dk) |

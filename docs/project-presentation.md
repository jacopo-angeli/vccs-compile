---
marp: true
title: VP2Pccs Presentation
paginate: true
theme: default
footer: "VP2Pccs: Value-Passing to Pure CCS Compiler"
---

# VP2Pccs

## A compiler from Value-Passing CCS to Pure CCS

**Speaker notes:** This talk covers the design and implementation of a compiler that transforms CCS specifications with value-passing into equivalent pure CCS specifications.

- Language: F#
- Type: console compiler
- Input: CCS with value passing and typed intervals
- Output: CCS with pure synchronisation only

---

# Outline (40 minutes)

1. **Process Algebra Fundamentals** (5 min)
2. **CCS: Calculus of Communicating Systems** (5 min)
3. **Value-Passing CCS** (5 min)
4. **The Transformation Problem** (5 min)
5. **Compiler Design & Implementation** (8 min)
6. **Correctness & Semantic Equivalence** (5 min)
7. **Demo & Results** (2 min)

---

# Part I: Process Algebra Fundamentals

---

# What is a Process?

A **process** is an abstract model of a system's behaviour:
- Executes a sequence of **actions** (events)
- Can communicate with other processes
- State is determined by its history
- Deterministic or non-deterministic transitions

## Why Process Algebra?

- **Compositional**: combine simple processes into complex systems
- **Algebraic**: reason about equivalences and laws
- **Formal verification**: prove correctness properties
- **Model-agnostic**: applies to hardware, software, protocols

---

# Key Concepts in Process Theory

### Action
An observable event: `a`, `b`, `send`, `receive`, etc.

### Transition
`P --a--> Q`: Process P can perform action a and become Q

### Trace
A sequence of actions: `a.b.c` represents the observable behaviour

### Equivalence
Two processes are equivalent if they exhibit the same observable behaviour

---

# Operational Semantics

Defines **how processes evolve** through transitions.

### Small-step semantics
Rules describe single transitions:

```
P --a--> P'     Q --b--> Q'
─────────────────────────────  (Par-Left)
(P | Q) --a--> (P' | Q)
```

### Transition system
A state machine where states are process terms and arcs are labelled with actions.

---

# Observation and Bisimulation

### Observable behaviour
Only visible actions matter; internal actions are abstracted away.

### Strong bisimulation
Two processes are bisimilar if they can mirror each other's actions indefinitely.

### Weak bisimulation
Allows internal (unobservable) steps to be abstracted.

**Central idea**: Equivalent processes have identical transition systems up to structural equivalence.

---

# Part II: CCS – Calculus of Communicating Systems

---

# CCS Overview

**CCS** (Milner 1980) is a foundational process calculus for reasoning about concurrent systems.

### Philosophy
- Communication is **synchronisation on channel names**
- Processes are built from **atomic actions** and **combinators**
- Behaviour is defined by **operational semantics**
- Equivalence is based on **bisimulation**

---

# CCS Syntax (Pure CCS)

```
P ::= 0                    (Nil/deadlock)
    | a.P                  (Prefix: action a then P)
    | P + P                (Sum: choice)
    | P | P                (Parallel: concurrent composition)
    | P \ A                (Restriction: hide channel set A)
    | P[f]                 (Relabelling/Renaming)
    | X                    (Variable/recursion)

a ::= c | c̅              (Action: input c or output c̅)
```

---

# CCS Semantics (Selected Rules)

### Prefix
```
a.P --a--> P
```

### Sum (Nondeterministic choice)
```
P --a--> P'           Q --a--> Q'
─────────────         ───────────
P + Q --a--> P'       P + Q --a--> Q'
```

### Parallel (Two processes synchronise on complementary actions)
```
P --a--> P'     Q --ā--> Q'
─────────────────────────────
P | Q --τ--> P' | Q'          (synchronisation produces silent action τ)
```

---

# CCS Semantics (Restriction)

### Restriction
```
P --a--> P'     a ∉ A ∧ ā ∉ A
─────────────────────────────
P \ A --a--> P' \ A
```

Channels in A cannot perform input or output; they are "hidden" from the environment.

### Renaming
```
P --a--> P'
──────────────
P[f] --f(a)--> P'[f]
```

Action a is relabelled according to function f.

---

# CCS Example: Producer-Consumer

```
Producer = 'send(x).τ.Producer
Consumer = send(msg).τ.Consumer
System = (Producer | Consumer) \ {send}
```

When Producer outputs on `send` and Consumer inputs on `send`, they synchronise:
```
Producer | Consumer --τ--> τ.Producer | τ.Consumer
```

The synchronisation is internal (τ) and invisible to the environment.

---

# Part III: Value-Passing CCS

---

# Motivation for Value-Passing

**Pure CCS limitation**: Communication happens on channel names only.

```
a.P  -- only synchronises on "a"
```

**Value-passing need**: Systems often exchange **data**.

```
send(x).P  -- send integer x on channel send, then do P
receive(y).Q  -- receive into variable y on channel receive, then do Q
```

**Problem**: Operational semantics becomes more complex; we must track variable bindings.

---

# VCCS Syntax: Value-Passing CCS

```
P ::= 0
    | a(x:T).P              (Input binding; x has type T)
    | 'a(E).P               (Output expression E on channel a)
    | if B then P else Q    (Conditional)
    | P + P                 (Sum)
    | P | P                 (Parallel)
    | P \ C                 (Restriction with typed channels)
    | P[f]                  (Renaming)
    | X(E₁,...,Eₙ)          (Parameterised constant call)
    | τ.P                   (Silent action)

T ::= [L, H]               (Interval type: integers from L to H)

E ::= n | x | E + E | E - E | ... (Arithmetic expressions)
B ::= E = E | E < E | ... ∧ ... | ... ∨ ...  (Boolean expressions)
```

---

# VCCS Semantics Sketch

Variables are tracked in an **environment** `Γ = {x ↦ 3, y ↦ 1, ...}`.

### Input rule
```
Γ ⊢ P --a(v)--> P'[v/x]   for all v ∈ domain(x)
──────────────────────────────
Γ ⊢ a(x).P --a(v)--> P'[v/x]
```

A single input action becomes a set of transitions, one per possible value.

### Output rule
```
Γ ⊢ E ⟹ v    (E evaluates to v under Γ)
──────────────────────────────
Γ ⊢ 'a(E).P --'a(v)--> P
```

---

# VCCS Example

```
Buffer = in(msg:[1,3]).out(msg).Buffer
```

Semantics: Buffer has three possible traces:
```
Buffer --in(1)--> out(1).Buffer
Buffer --in(2)--> out(2).Buffer
Buffer --in(3)--> out(3).Buffer
```

Output carries the received value forward.

---

# Part IV: The Transformation Problem

---

# Why Transform VCCS to PCCS?

### Problem with VCCS semantics
- Operational semantics is complex: need to track environments
- Model-checking becomes harder: infinite datatypes (e.g., integers)
- Tools for pure CCS are mature and optimised

### Opportunity
- Value domains are **finite** (bounded by interval types)
- We can **enumerate all possibilities** at compile-time
- Transform into pure PCCS ("Pure CCS") where actions are ground

---

# Core Transformation Idea

### Input action
```
a(x:[0,10]).P
```

becomes

```
a_0.P[0/x] + a_1.P[1/x] + ... + a_10.P[10/x]
```

**Insight**: The choice of value is represented as a choice of channel name.

### Output action
```
'a(x+1).P     where x = 3
```

becomes

```
'a_4.P
```

**Insight**: Expression evaluation happens at compile-time; the result is embedded in the action name.

---

# Handling Parameterised Constants

### Source
```
Q(x:[0,5], y:[0,2]) = 'out(x*y).Q(x,y)
B = Q(2,1)
```

### Target (selected instances)
```
Q_2_1 = 'out_2.Q_2_1
Q_0_0 = 'out_0.Q_0_0
...
B = Q_2_1
```

Each combination of parameter values becomes a distinct constant in the target.

---

# Cartesian Product Enumeration

To compile a process with parameters:

```
P(x:[L₁,H₁], y:[L₂,H₂], ...) = ...
```

Generate all valuations:

```
{ (x, y, ...) | L₁ ≤ x ≤ H₁, L₂ ≤ y ≤ H₂, ... }
```

**Complexity**: If there are n parameters each with domain size d, we generate d^n process definitions.

---

# Expansion Example: Conditional

### Source
```
Cond(x:[0,2]) = if x > 0 then 'tick.Cond(x) else 0
```

### Target
```
Cond_0 = 0
Cond_1 = 'tick.Cond_1
Cond_2 = 'tick.Cond_2
```

At compile-time, the boolean is evaluated for each concrete value of x.

---

# Semantic Preservation

**Claim**: The transformation is semantics-preserving – the PCCS output exhibits the same observable behaviour as the VCCS input.

### Informal argument
- Each VCCS transition corresponds to one PCCS transition
- The choice of value in input is reflected as a choice among sum branches in PCCS
- Pure CCS actions are fully ground; no environments needed

### Formal proof (sketch)
Would require defining a bisimulation relation between VCCS and PCCS terms under environment valuations, then showing the transformation respects it.

---

# Part V: Compiler Design & Implementation

---

# Problem

- Value-passing CCS is expressive, but the communication values have to be carried in the operational model.
- Pure CCS only synchronises on channel names.
- This project compiles a value-passing specification into an equivalent pure CCS specification by expanding values into channel-specific actions.

### Core idea

`a(x:[0,10]).P` becomes a sum over concrete channels:

`a_0.P[0/x] + a_1.P[1/x] + ... + a_10.P[10/x]`

---

# Compiler Architecture

```
┌──────────────┐
│   Parser     │  (fslex/fsyacc)
│ VCCS syntax  │
└──────┬───────┘
       │
       v
┌──────────────────┐
│  VCCS AST        │
│ (untyped terms)  │
└──────┬────────────┘
       │
       v
┌──────────────────────────────┐
│  Compiler.fs                 │
│  - Evaluate expressions      │
│  - Enumerate domains         │
│  - Generate PCCS definitions │
└──────┬───────────────────────┘
       │
       v
┌──────────────────┐
│   PCCS AST       │
│ (ground actions) │
└──────┬────────────┘
       │
       v
┌──────────────────┐
│ Pretty-Printer   │
│ (stringify)      │
└──────┬────────────┘
       │
       v
┌──────────────────┐
│  Output file     │
│  (*_ccs.txt)     │
└──────────────────┘
```

---

# Project Structure

- `compiler/src/Program.fs`: CLI entry point and file IO
- `compiler/src/Compiler.fs`: translation logic from VCCS to PCCS
- `compiler/src/Vccs/fslexyacc`: lexer and parser definitions
- `compiler/src/Vccs/Types`: source-language AST
- `compiler/src/Pccs/Types`: target-language AST and pretty-printer
- `test`: example inputs and expected compiled output

---

# Compilation Pipeline

1. Read input file
2. Lex and parse into VCCS declarations
3. Compile each declaration into one or more PCCS declarations
4. Expand value domains into explicit channel names
5. Pretty-print the resulting pure CCS program
6. Save to `<input>_ccs.txt`

### Entry point

The CLI expects:

`V2Pccs <inputfile>`

---

# Key Implementation Details

### Expression evaluation
```fsharp
let rec evaluateA (exp: Vccs.AExp) : int =
    match exp with
    | Num x -> x
    | Var x -> 
        match List.find (fun (name, _) -> name = x) env with
        | (_, value) -> value
    | Add (l, r) -> evaluateA l + evaluateA r
    ...
```

Arithmetic and boolean expressions are evaluated in an **environment** that tracks current variable bindings.

---

# Cartesian Product in F#

```fsharp
let rec cartesianProduct (domains: (string * int list) list) : (string * int) list list =
    match domains with
    | [] -> [ [] ]  
    | (name, values) :: rest ->
        let restCombinations = cartesianProduct rest
        [ for value in values do
            for combo in restCombinations ->
                (name, value) :: combo
        ]
```

Generates all combinations of parameter values.

---

# Translation Rules: Actions

### Input
```
Γ ⊢ a(x:[L,H]).P  ⟹  ∑(i=L to H) a_i.P' where P' is P with x ↦ i in Γ
```

### Output
```
Γ ⊢ 'a(E).P  ⟹  'a_v.P  where E evaluates to v under Γ
```

### Silent
```
τ.P  ⟹  τ.P
```

---

# Translation Rules: Operators

### Conditional
```
Γ ⊢ if B then P else Q  ⟹  P'  if B is true under Γ
Γ ⊢ if B then P else Q  ⟹  Q'  if B is false under Γ
```

### Parallel, Sum, etc.
Recursively translate subterms.

### Restriction
```
Γ ⊢ P \ {c₁:[L₁,H₁], ...}  ⟹  P' \ {c₁_L₁, ..., c₁_H₁, ...}
```

Expand all concrete channel names in the restriction set.

---

# Implementation Notes

- The compiler computes Cartesian products of parameter domains to generate all concrete valuations.
- Arithmetic expressions are evaluated when enough bindings exist in the current environment.
- The target representation is a clean PCCS AST with a dedicated stringifier.
- Undefined constant references are detected after compilation and reported as warnings.

---

# Part VI: Correctness & Semantic Equivalence

---

# What Does Correctness Mean?

**Preservation**: The transformation preserves the observable behaviour of the process.

If VCCS process P has a trace `a.b.c`, then the compiled PCCS process P_ccs has a corresponding trace in the target language.

### Two dimensions
1. **Finite branching**: Infinite choice (over integers) becomes finite (enumerated branches)
2. **Grounding**: Symbolic values become concrete action names

---

# Bisimulation Argument

Two processes are **bisimilar** if their transition systems are isomorphic (have the same shape).

### For VCCS / PCCS
We sketch a bisimulation between:
- VCCS term P with environment Γ
- PCCS term (result of compiling P under Γ)

The bisimulation witness is the identity on action traces after value substitution.

---

# Correctness Properties

### Soundness
Every trace of compiled PCCS corresponds to a trace of original VCCS.

### Completeness
Every trace of VCCS is captured in compiled PCCS.

### Finiteness guarantee
If all interval types are bounded, the compiled PCCS is finite (no infinite branching).

---

# Limitations and Caveats

1. **Domain explosion**: Large intervals generate many process definitions
   - Example: `x:[0,100], y:[0,100]` creates 10,001 definitions

2. **Symbolic reasoning lost**: The transformation discards symbolic relationships
   - Cannot exploit `x + y < 100` for optimisation

3. **Untyped target**: PCCS has no notion of "which channels are related by value"
   - Tool-assisted reverse engineering is harder

---

# Optimisation Opportunities

### State-space reduction
- Symbolic encoding of groups of channels
- Partial evaluation to detect dead code

### Interval analysis
- Detect unreachable valuations and prune them

### Automata minimization
- Apply bisimulation minimisation post-compilation

---

# Part VII: Demo & Results

---

# Build And Run

## Compile

```bash
cd compiler
dotnet build V2Pccs.fsproj
```

## Run

```bash
dotnet run --project V2Pccs.fsproj ../test/full.txt
```

## Output

- The generated file is written next to the input file
- Example: `full.txt` -> `full_ccs.txt`

---

# Worked Example

## Source

```text
P = a(x:[0,10]).Q(x);
Q(x:[0,1], y:[0,5]) = Q(x);
B = Q(1);
```

## Compiled result (excerpt)

```text
P = (a_0.Q_0 + a_1.Q_1 + ... + a_10.Q_10)
Q_0_0 = Q_0
Q_0_1 = Q_0
...
Q_1_5 = Q_1
B = Q_1
```

---

# Example From Test Suite: Complex Compilation

## Input process

```text
InputProc = channel(msg:[0,20]).P;
OutputProc = channel(x:[10,20]).'channel(x + 1).Q(x);
```

## Output processes

```text
InputProc = channel_0.P + channel_1.P + ... + channel_20.P
OutputProc = (channel_10.'channel_11.Q_10 + ... + channel_20.'channel_21.Q_20)
```

---

# Strengths

- Clear separation between parser, AST types, compiler, and pretty-printer
- Simple CLI workflow
- Concrete test examples already included
- Translation is explicit and easy to inspect

## Current limitations

- Domain expansion can grow quickly for large intervals
- Build currently emits F# warnings that could be cleaned up
- README is minimal and could document syntax and usage in more depth

---

# Future Improvements

- Add a formal proof of semantic preservation
- Implement state-space reduction heuristics
- Improve diagnostics for parse and compilation errors
- Add more sample programs and automated tests
- Reduce output blow-up with smarter encodings or optimisations
- Extend documentation with syntax reference and examples

---

# Summary

- **Process algebra** provides a foundation for reasoning about concurrent systems
- **CCS** is a fundamental calculus with clean semantics
- **Value-passing CCS** extends CCS to handle data but complicates the model
- **VP2Pccs** solves the problem by expanding value domains into distinct channels
- The transformation is semantics-preserving and enables model-checking on finite systems
- Implementation is clean, modular, and buildable with modern tooling

---

# Questions?

**Contact**: The VP2Pccs project is available in the L4CandD-project repository.

**Key takeaway**: Finite-domain value passing can be compiled away through systematic enumeration, preserving observable behaviour while enabling standard process-calculus tools.

- Value-passing CCS is expressive, but the communication values have to be carried in the operational model.
- Pure CCS only synchronises on channel names.
- This project compiles a value-passing specification into an equivalent pure CCS specification by expanding values into channel-specific actions.

### Core idea

`a(x:[0,10]).P` becomes a sum over concrete channels:

`a_0.P[0/x] + a_1.P[1/x] + ... + a_10.P[10/x]`

---

# What The Tool Does

- Parses a source file written in the project VCCS syntax
- Evaluates arithmetic and boolean expressions
- Enumerates parameter valuations from interval types
- Rewrites actions, conditionals, restrictions, renamings, and constant calls
- Writes the compiled result to a new `_ccs` file beside the input

---

# Project Structure

- `compiler/src/Program.fs`: CLI entry point and file IO
- `compiler/src/Compiler.fs`: translation logic from VCCS to PCCS
- `compiler/src/Vccs/fslexyacc`: lexer and parser definitions
- `compiler/src/Vccs/Types`: source-language AST
- `compiler/src/Pccs/Types`: target-language AST and pretty-printer
- `test`: example inputs and expected compiled output

---

# Compilation Pipeline

1. Read input file
2. Lex and parse into VCCS declarations
3. Compile each declaration into one or more PCCS declarations
4. Expand value domains into explicit channel names
5. Pretty-print the resulting pure CCS program
6. Save to `<input>_ccs.txt`

### Entry point

The CLI expects:

`V2Pccs <inputfile>`

---

# Translation Strategy

## Input actions

`channel(x:[0,2]).P`

becomes

`channel_0.P[0/x] + channel_1.P[1/x] + channel_2.P[2/x]`

## Output actions

`'channel(x + 1).P`

becomes a concrete output after expression evaluation, for example:

`'channel_4.P`

---

# Translation Strategy

## Conditionals

- Boolean expressions are evaluated during compilation once variables have concrete values.
- A conditional becomes only its selected branch.

## Constant parameters

- Parameterised process constants are expanded into concrete names.
- Example: `Q(1, 3)` becomes `Q_1_3`

## Restriction and renaming

- Typed channels are expanded to all concrete channel names before applying the operator.

---

# Worked Example

## Source

```text
P = a(x:[0,10]).Q(x);
Q(x:[0,1], y:[0,5]) = Q(x);
B = Q(1);
```

## Compiled result

```text
P = (a_0.Q_0 + a_1.Q_1 + ... + a_10.Q_10)
Q_0_0 = Q_0
...
Q_1_5 = Q_1
B = Q_1
```

---

# Example From Test Suite

## Input process

```text
InputProc = channel(msg:[0,20]).P;
```

## Output process

```text
InputProc = channel_0.P + channel_1.P + ... + channel_20.P
```

This shows the central transformation: values are no longer transmitted as data; they are encoded into the action label itself.

---

# Implementation Notes

- The compiler computes Cartesian products of parameter domains to generate all concrete valuations.
- Arithmetic expressions are evaluated when enough bindings exist in the current environment.
- The target representation is a clean PCCS AST with a dedicated stringifier.
- Undefined constant references are detected after compilation and reported as warnings.

---

# Build And Run

## Compile

```bash
cd compiler
dotnet build V2Pccs.fsproj
```

## Run

```bash
dotnet run --project V2Pccs.fsproj ../test/full.txt
```

## Output

- The generated file is written next to the input file
- Example: `full.txt` -> `full_ccs.txt`

---

# Strengths

- Clear separation between parser, AST types, compiler, and pretty-printer
- Simple CLI workflow
- Concrete test examples already included
- Translation is explicit and easy to inspect

## Current limitations

- Domain expansion can grow quickly for large intervals
- Build currently emits F# warnings that could be cleaned up
- README is minimal and could document syntax and usage in more depth

---

# Future Improvements

- Add a formal statement of semantic preservation
- Improve diagnostics for parse and compilation errors
- Add more sample programs and automated tests
- Reduce output blow-up with smarter encodings or optimisations
- Extend documentation with syntax reference and examples

---

# Summary

- VP2Pccs translates value-passing CCS into pure CCS
- The compiler works by enumerating possible values and embedding them into channel names
- The implementation is modular, readable, and already buildable with the .NET CLI
- The project is a good base for both coursework presentation and further formal methods work

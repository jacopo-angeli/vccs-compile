# VP2Pccs

A compiler from CCS with Value Passing to CCS with Pure Synchronisation.
Written in F# as a modular console application.

---

## 🚀 Overview

**VP2Pccs** is a tool that transforms CCS processes with *value-passing* into equivalent CCS processes that use *pure synchronization only*.
It supports typed variables using integer ranges and generates `.ccs` files where each possible communication value is expanded into a distinct synchronization channel.

<samp>

# Chrysippus | Propositional Knowledge-base
> Experimental project
## Overview
Knowledge representation.
It can be used to solve problems that can be represented with systems of propositional logic, where the "facts" can be encoded with symbols and variables of the Boolean type.
- [Propositional calculus](https://en.wikipedia.org/wiki/Propositional_calculus)
- [Knowledge base](https://en.wikipedia.org/wiki/Knowledge_base)
## Syntax
### Operator
| Symbol | Description | Example    |
|:------:|:-----------:|:-----------|
|   &    |     And     | A & E      |
| &#124; |     Or      | A &#124; B |
|   ~    |     Not     | ~A         |
|   =>   |    Imply    | E => C     |
|  <=>   | If Only If  | E <=> C    |
### Valid tokens
```(```, ```)```, ``` ``` (space)
### Examples
```A & E```<br/>
```~A | B```<br/>
```A | (B & C)```<br/>
```(C & A) <=> (B & C)```
## How to use
Create a propositional knowledge base, choose evaluation strategy, tell '*facts*', make questions and retract these if needed.
```csharp
...
using PropLogic.Kb;
using PropLogic.Kb.Evaluator;
...
_kb = new Kb();
_kb.UseStrategy(PropositionalEvaluateStrategyType.ProofByContradiction);

_kb.Tell("A & E");

Assert.Equal(InquireResponse.True, _kb.Ask("A"));
Assert.Equal(InquireResponse.True, _kb.Ask("E"));

_kb.Tell("E => C"); // ~E | C

Assert.Equal(InquireResponse.True, _kb.Ask("C"));

_kb.Retract("E");

Assert.Equal(InquireResponse.False, _kb.Ask("E"));
Assert.Equal(InquireResponse.False, _kb.Ask("C"));
...
```
## License
[MIT](/license)

</samp>
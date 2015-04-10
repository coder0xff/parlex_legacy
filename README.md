parlex
======

A collection of tools for the design of programming languages.

More generally speaking, parlex is an effort to improve the available tooling for designing context-free language grammars, which is often an important step in creating a new programming language. The most common tools, such as flex and bison, and even the more advanced ones, such as ANTLR and GOLD, fall short of the sophistication possible. The academic community has researched the areas of state machines, context free languages, and behavior trees extensively, yet much of the knowledge is left untapped in the very tools that are used to work with such data structures. Parlex aims to improve upon the state of the art by implementing, at a minimum, the following functionality:

 - Graphical representations of the behavior of grammars
 - Fully automated optimization and minimization of grammars
 - Cross conversion between common grammars such as GLR, LALR, and LR(k).
 - Cross conversion between common metasyntaxes such as EBNF, Wirth Syntax Notation, and Regular Expressions
 - A framework for parallel non-deterministic parsing of text based on a supplied grammar definition
 - An Integrated Development Environment

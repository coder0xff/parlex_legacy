parlex
======

A parser generator, led by example.

Parlex creates grammar definitions in an intuitive manner with a minimal learning curve. The user creates examples of valid syntax called exemplars. Then, the user chooses spans of text in the exemplar, and associates one or more grammar products with each span. This information is used to construct an NFA that can recognize the patterns. For example, an exemplar, where the first line is the text, and the following lines are products (note that github removes the necessary carriage returns and spaces - so, view the raw file):

a = (b + 5) * 3.14
| : identifier
| : expression
         | : integer_constant
     | : identifier
              |  | : expression
     | : expression
         | : expression  	      
|                | : statement
|                | : assignment
    |     | : parenthetical_expression
     |   | : addition
     |   | : expression
    |     | : expression
    |            | : multiplication
    |            | : expression
              |  | : float_constant
              
Parlex will, tentatively, also include an editor for editing the .ple files. It is not a text editor. Exemplars are listed, and once selected, each product in an exemplar can be edited using specialized GUI controls. In the editor, matches that can be identified using existing exemplars are displayed as the user types in a new exemplar. The user then adds any missing products.

Parlex will also be able to identify ambiguities. More importantly, parlex will generate text that demonstrates the ambiguity, along with it's possible products using the editors control scheme. No more confusing shift-reduce and reduce-reduce errors.

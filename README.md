parlex
======

A parser generator, led by example.

Parlex is an effort to create grammar definitions using a completely intuitive interface. The user, likely someone who is making a programming language, creates examples of valid syntax called exemplars. Then, the user chooses spans of text in the exemplar, and associates one or more products with it. This information is used to construct an NFA that can recognize the patterns. For example, an exemplar, where the first line is the text, and the following are products (note that github removes the necessary carriage returns and spaces - so, view the raw file):

a = (b + 5) * 3.14
         | : integer_constant
     | : identifier
              |  | : expression
     | : expression
         | : expression  	      
| : identifier
|                | : statement
|                | : assignment
    |     | : parenthetical_expression
     |   | : addition
     |   | : expression
    |     | : expression
    |            | : multiplication
    |            | : expression
              |  | : float_constant
              
Parlex will, tentatively, also include an editor for editing the .ple files. It is not a text editor. Exemplars are listed as items, and each product can be edited using a specialized control for adjusting its boundaries. In the editor, matches that can be identified using existing exemplars are displayed as the user types in a new exemplar (if the whole text can be matched as a product). The user then adds any missing products.

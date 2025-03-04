grammar DeltinScript;

/*
 * Parser Rules
 */

number : NUMBER | neg  ;
neg    : '-'NUMBER     ;
string : STRINGLITERAL ;
formatted_string: '<' string (COMMA expr)* '>' ;
true   : TRUE          ;
false  : FALSE         ;
null   : NULL          ;

statement_operation : EQUALS | EQUALS_ADD | EQUALS_DIVIDE | EQUALS_MODULO | EQUALS_MULTIPLY | EQUALS_POW | EQUALS_SUBTRACT ;

vardefine : DEFINE (GLOBAL|PLAYER) PART (PART (INDEX_START number INDEX_END)?)? STATEMENT_END; /*#define global/player NAME (use variable?); */
define   : DEFINE PART (EQUALS expr)? ;
useGlobalVar : USEVAR GLOBAL PART STATEMENT_END ;
usePlayerVar : USEVAR PLAYER PART STATEMENT_END ;

expr 
	: 
      number                                      // Numbers
	| method                                      // Methods
	| string                                      // Strings
	| enum                                        // Enums
	| expr INDEX_START expr INDEX_END             // Array creation
	| createarray
	| formatted_string                            // Formatted strings
	| true                                        // True
	| false                                       // False
	| null                                        // Null
	| variable                                    // Variables
	| exprgroup
	| expr SEPERATOR variable                     // Variable seperation
	| <assoc=right> expr '^' expr                 // x^y
	| expr '*' expr                               // x*y
	| expr '/' expr                               // x/y
	| expr '%' expr                               // x%y
	| expr '+' expr                               // x+y
	| expr '-' expr                               // x-y
	| NOT expr                                     // !x
	| expr ('<' | '<=' | '==' | '>=' | '>' | '!=') expr // x == y
	| expr BOOL expr                              // x & y
	;

exprgroup   : LEFT_PAREN expr RIGHT_PAREN ;
createarray : INDEX_START (expr (COMMA expr)*)? INDEX_END;

array : INDEX_START expr INDEX_END ;

enum : ENUM SEPERATOR PART? ;

variable : PART ;
// define   : DEFINE PART (EQUALS expr)? STATEMENT_END ;
varset   : (expr SEPERATOR)? PART array? statement_operation expr ;
// Here, there should always be an expression. 
// This is here so antlr recognizes it is a method midtype.
// Confirm there is an expression in the visitor class.
//                            V
parameters : expr (COMMA expr?)*    		 	     ;
method     : PART LEFT_PAREN parameters? RIGHT_PAREN ;

statement :
	( varset STATEMENT_END
	| method STATEMENT_END?
	| if
	| for
	| while
	| define STATEMENT_END
	| return
	);

block : (BLOCK_START statement* BLOCK_END) | statement | STATEMENT_END  ;

for     : FOR LEFT_PAREN 
	((PART IN expr) | ((define | varset)? STATEMENT_END expr? STATEMENT_END statement?))
	RIGHT_PAREN block;

while   : WHILE LEFT_PAREN expr RIGHT_PAREN block             ;

if      : IF LEFT_PAREN expr RIGHT_PAREN block else_if* else? ;
else_if : ELSE IF LEFT_PAREN expr RIGHT_PAREN block           ;
else    : ELSE block                                          ;

return  : RETURN expr? STATEMENT_END                          ;

rule_if : (IF LEFT_PAREN (expr (COMMA expr)*) RIGHT_PAREN)*;

// rule_option{0,3} does not work
ow_rule : 
	RULE_WORD ':' STRINGLITERAL rule_option? rule_option? rule_option?
	rule_if
	block
	;

rule_option: PART SEPERATOR PART? ;

user_method : METHOD PART LEFT_PAREN (PART (COMMA PART)*)? RIGHT_PAREN
	block
	;

ruleset :
	useGlobalVar?
	usePlayerVar?
	(vardefine | ow_rule | user_method)*
	;

/*
 * Lexer Rules
 */

fragment LOWERCASE  : [a-z] ;
fragment UPPERCASE  : [A-Z] ;
fragment NUMBERS    : [0-9] ;

// Strings have priorty over everything!
STRINGLITERAL             : UNTERMINATEDSTRINGLITERAL '"'      ;
UNTERMINATEDSTRINGLITERAL : '"' (~["\\\r\n] | '\\' (. | EOF))* ;

// Comments
COMMENT : (('/*' .*? '*/') | ('//' .*? NEWLINE))? -> skip ;

// Misc
WHITESPACE : (' '|'\t')+ -> skip ;
NEWLINE    : ('\r'? '\n' | '\r')+ -> skip;

// Goto statement
/* split this into a parse statement later */
GOTO           : '@goto' WHITESPACE+ PART ';' ;
GOTO_STATEMENT :  'goto' WHITESPACE+ PART ';' ;

// Numbers
NUMBER : [0-9]+ ('.'[0-9]+)?  ;

// Groupers
LEFT_PAREN    : '(' ;
RIGHT_PAREN   : ')' ;
BLOCK_START   : '{' ;
BLOCK_END     : '}' ;
INDEX_START   : '[' ;
INDEX_END     : ']' ;
STATEMENT_END : ';' ;
SEPERATOR     : '.' ;
COMMA         : ',' ;

// Keywords
RULE_WORD : 'rule'      ;
IF        : 'if'        ;
ELSE      : 'else'      ;
FOR       : 'for'       ;
IN        : 'in'        ;
DEFINE    : 'define'    ;
USEVAR    : 'usevar'    ;
PLAYER    : 'playervar' ;
GLOBAL    : 'globalvar' ;
TRUE      : 'true'      ;
FALSE     : 'false'     ;
NULL      : 'null'      ;
ARRAY     : 'array'     ;
METHOD    : 'method'    ;
RETURN    : 'return'    ;
WHILE     : 'while'     ;

EQUALS          : '='  ;
EQUALS_POW      : '^=' ;
EQUALS_MULTIPLY : '*=' ;
EQUALS_DIVIDE   : '/=' ;
EQUALS_ADD      : '+=' ;
EQUALS_SUBTRACT : '-=' ;
EQUALS_MODULO   : '%=' ;
BOOL : '&' | '|';
NOT : '!';

ENUM : (LOWERCASE | UPPERCASE | '_' | NUMBERS)+ { Deltin.Deltinteger.Elements.EnumData.IsEnum(Text) }?;
PART : (LOWERCASE | UPPERCASE | '_' | NUMBERS)+ ;
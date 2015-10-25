grammar fsharp_ss;
 
options {
	language=CSharp3;
	output=AST;
	backtrack=true;
}

tokens {
	UNKNOWN;
	LET = 'let';
	AND = '&&';
	OR = '||';
	IF = 'if';
	THEN = 'then';
	ELIF = 'elif';
	ELSE = 'else';
	TRUE = 'true';
	FALSE = 'false';
	MATCH = 'match';
	WHEN = 'when';
	MUTABLE = 'mutable';
	REC = 'rec';
	FUN = 'fun';
	BEGIN = 'begin';
	END = 'end';
	STRING_KW = 'string';
	CHAR_KW = 'char';
	INT_KW = 'int';
	DOUBLE_KW = 'double';
	BOOL_KW = 'bool';
	//PRINTF = 'printf';
	//SCANF = 'scanf';
	ASSIGN = '<-';
	FUN_DEF = '->';
	PLUS = '+';
	MINUS = '-';
	MULT = '*';
	DIV = '/';
	MOD = '%';
	EQ = '=';
	NEQ = '!=';
	GT = '>';
	LT = '<';
	GE = '>=';
	LE = '<=';
	PIPE = '|>';
	TAB = '\t';
	OPEN_BR = '(';
	CLOSE_BR = ')';
	
	ENTRY_POINT = '[<EntryPoint>]';
	
	VALUE_DEFN = 'VALUE_DEFN';
	FUNCTION_DEFN = 'FUNCTION_DEFN';
	ARGS = 'ARGS';
	TYPE = 'TYPE';
	BODY = 'BODY';
	FUNCTION_CALL = 'FUNCTION_CALL';
	PROGRAM = 'PROGRAM';
	EXPR = 'EXPR';
	NAME = 'NAME';
}

@lexer::namespace { fsharp_ss }
@lexer::members {const int HIDDEN = Hidden;}
@parser::namespace { fsharp_ss }

ID  :	('a'..'z'|'A'..'Z'|'_') ('a'..'z'|'A'..'Z'|'0'..'9'|'_')*
    ;

INT :	'0'..'9'+
    ;

DOUBLE	:   ('0'..'9')+ '.' ('0'..'9')* EXPONENT? |
	    '.' ('0'..'9')+ EXPONENT? |
            ('0'..'9')+ EXPONENT	
	;
	
EXPONENT : ('e'|'E') ('+'|'-')? ('0'..'9')+ ;

STRING
    :  '"' ( ESC_SEQ | ~('\\'|'"') )* '"'
    ;

CHAR:  '\'' ( ESC_SEQ | ~('\''|'\\') ) '\''
    ;

fragment
HEX_DIGIT : ('0'..'9'|'a'..'f'|'A'..'F') ;

fragment
ESC_SEQ
    :   '\\' ('b'|'t'|'n'|'f'|'r'|'\"'|'\''|'\\')
    |   UNICODE_ESC
    |   OCTAL_ESC
    ;

fragment
OCTAL_ESC
    :   '\\' ('0'..'3') ('0'..'7') ('0'..'7')
    |   '\\' ('0'..'7') ('0'..'7')
    |   '\\' ('0'..'7')
    ;

fragment
UNICODE_ESC
    :   '\\' 'u' HEX_DIGIT HEX_DIGIT HEX_DIGIT HEX_DIGIT
    ;
    
const	:	
	INT | DOUBLE | STRING | CHAR | TRUE | FALSE
	;

type	:
	 STRING_KW |
	 CHAR_KW |
	 INT_KW |
	 DOUBLE_KW |
	 BOOL_KW
	;

value_defn
	:
	MUTABLE? ID return_type? '=' body_expr
		-> ^(VALUE_DEFN ^(NAME ID) MUTABLE? ^(TYPE return_type?) body_expr)
	;

function_defn
	:
	REC? ID function_args return_type? '=' body_expr	
		-> ^(FUNCTION_DEFN ^(NAME ID) REC? ^(ARGS function_args) ^(TYPE return_type?) body_expr)
	;	
	
function_args
	:
	ID+ | 
	'('! ')'! |
	('('! ID^ ':'! type ')'!)+	
	;
	
return_type
	:	
	':'! type
	;	
	
body_expr
	:
	expr_block
		-> ^(BODY expr_block)
	;
	
if_expr	:
	IF^ logic_expr THEN! expr_block elif_expr* else_expr?
	;

elif_expr
	:
	ELIF^ logic_expr THEN! expr_block
	;
	
else_expr
	:
	ELSE! expr_block
	;

alg_group_expr
	:
	'('! add_expr ')'! |
	func_call_expr | ID | const
	;

mult_expr
	:
	alg_group_expr ((MULT | DIV | MOD)^ alg_group_expr)*	
	;

add_expr:
	mult_expr ((PLUS | MINUS)^ mult_expr)*
	;

alg_expr:
	add_expr	
	;

comp_expr
	:
	eq_neq_expr |
	comp_expr_arg comp_operation^ comp_expr_arg
	;
	
comp_operation
	:
	GT | LT | GE | LE	
	;

comp_expr_arg
	:
	INT | DOUBLE | alg_expr | func_call_expr | ID
	;

eq_neq_expr
	:
	eq_neq_expr_arg (EQ | NEQ)^ eq_neq_expr_arg
	;
	
eq_neq_expr_arg
	:
	STRING | CHAR | INT | DOUBLE | alg_expr | func_call_expr | ID
	;

logic_expr_arg
	:
	TRUE | FALSE | comp_expr | '('! or_expr ')'! | func_call_expr | ID
	;

and_expr:
	logic_expr_arg (AND^ logic_expr_arg)*
	;

or_expr	:
	and_expr (OR^ and_expr)*	
	;	

logic_expr
	:
	or_expr	
	;

func_call_expr
	:
	ID (OPEN_BR returning_expr* CLOSE_BR)+
		-> ^(FUNCTION_CALL ^(NAME ID) ^(ARGS returning_expr*))
	;

expr_block
	:
	BEGIN^ expr_list END!
	;

returning_expr
	:
	'('! returning_expr ')'! |
	FUN function_args FUN_DEF body_expr
		-> ^(FUNCTION_DEFN ^(ARGS function_args) body_expr) |
	if_expr |
	alg_expr |
	logic_expr |
	const |
	ID
	;
	
expr	:
	returning_expr ';'	
		-> returning_expr |
	LET! function_defn | 
	LET! value_defn
	;
	
expr_list
	:
	expr*
	;

public execute
	:
	expr_list
		-> ^(PROGRAM expr_list)
	;
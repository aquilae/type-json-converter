TypeJsonConverter
===================

JsonConverter implementation for serializing/deserializing Type objects in Json.NET

Usage
-----

    new JsonSerializerSettings().Converters.Add(new TypeJsonConverter())

Constructor arguments
---------------------

    TypeJsonConverter()
Sets initial cache capacity to 8 items

    TypeJsonConverter(int initialCacheCapacity)
Sets initial cache capacity to supplied value

    TypeJsonConverter(IEnumerable<KeyValuePair<string, Type>> cache)
Fills cache with supplied values (sets initial capacity to 32)

Storage format
--------------

<h3>Glossary:</h3>

    <type>          - supplied type
    <type name>     = <type>.FullName
    <assembly name> = <type>.Assembly.FullName
  
<br>
There are three possible formats of generated JSON

<h3>For simple non-generic types:</h3>

    {"hash"         : <string>,
     "type"         : 1,
     "assemblyName" : <string>,
     "typeName"     : <string>}
    
    hash            = "1:<assembly name>:<type name>"
    assemblyName    - <assembly name>
    typeName        - <type name>
    
<h3>For generic types:</h3>

    {"type"         : 2,
     "assemblyName" : <string>,
     "typeName"     : <string>,
     "genericDef"   : <type json>,
     "genericArgs"  : [<type json>]}
    
    assemblyName    - <assembly name>
    typeName        - <type name>
    genericDef      - serialized definition type
    genericArgs     - serialized generic arguments types
    
<h3>For generic definitions:</h3>

    {"hash"         : <string>,
     "type"         : 3,
     "assemblyName" : <string>,
     "typeName"     : <string>,
     "genericArity" : int}
    
    <generic arity> = <type>.GetGenericArguments().Length
    hash            = "3:<assembly name>:<type name>:<generic arity>"
    assemblyName    - <assembly name>
    typeName        - <type name>
    genericArity    - <generic arity>

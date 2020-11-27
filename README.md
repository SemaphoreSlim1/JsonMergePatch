# Introduction 
Json Merge Patch Support for .Net 5

[![Build Status](https://dev.azure.com/matthewethomas/Public%20Projects/_apis/build/status/JsonMergePatch?branchName=master)](https://dev.azure.com/matthewethomas/Public%20Projects/_build/latest?definitionId=13&branchName=master)
![code coverage](https://img.shields.io/azure-devops/coverage/matthewethomas/Public%2520Projects/13?style=flat-square)

## Json Merge Patch
- [RFC 7396](https://tools.ietf.org/html/rfc7396)
- Backed by Newtonsoft.Json or System.Text.Json - your choice
- Performs partial resource updates similar to JSON Patch
- Supports Swagger
- .Net 5

Some inspiring projects:
- [Here's a introduction to merge patch](http://blog.primarilysoftware.com/2019/json-merge-patch-dot-net/)
- [Morcatko's JsonMergePatch](https://github.com/Morcatko/Morcatko.AspNetCore.JsonMergePatch)

``` C#
// C# Object
public class Person
{
    public string FirstName {get; set;}
    public string LastName {get; set;}
}
```

``` JSON
// JSON Merge Patch
{
 "LastName": "Smith"
}
```

``` C#
// Apply all the changes in the patch to your existing DTO
[HttpPatch]
[Consumes(MergePatchDocument.ContentType)]
public IActionResult Patch([FromBody] IJsonMergePatch<Person> patch)
{
    ...
    patch.ApplyTo(existingDTO);
    ...
}
```

``` C#
//If you need to act based on the presence/absence of a property in the patch
[HttpPatch]
[Consumes(MergePatchDocument.ContentType)]
public IActionResult Patch([FromBody] IJsonMergePatch<Person> patch)
{
    if(lastName.TryGetValue(x => x.LastName, out var ln)
    {
        //act accordingly
    }
}
```

``` C#
// If you need to create a patch document to call a different service
public void CreatePatch()
{
    //simple sets, one property at a time
    var patch = JsonMergePatch.New();
    patch.Set(x => x.LastName, "Smith");

    //or, use a builder, which can conditionally set values from other merge patchess
    var pb = JsonMergePatch.CreateBuilder<Person>();
    pb.Set(x => x.LastName).ToValue("Smith");
    var patch = pb.Build();
}
```
using Example.Model;
using JsonMergePatch.Core;
using JsonMergePatch.NewtonsoftJson;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace Example.Newtonsoft.Inbound.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PeopleController : ControllerBase
    {
        private readonly ILogger<PeopleController> _logger;

        private readonly Person[] _people = new Person[]
        {
            new Person { FirstName = "John", LastName = "Doe"},
            new Person { FirstName = "Jane", LastName = "Doe"}
        };

        public PeopleController(ILogger<PeopleController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IEnumerable<Person> Get()
        {
            return _people;
        }

        [HttpGet("{id}")]
        public Person Get(int id)
        {
            var pd = new JsonPatchDocument<Person>();
            pd.Replace(x => x.FirstName, "John");

            return _people.First();
        }

        [HttpPatch]
        [Consumes(JsonMergePatch.NewtonsoftJson.JsonMergePatch.ContentType)]
        public IActionResult Patch([FromBody] IJsonMergePatch<Person> patch)
        {


            if (patch.TryGetValue(x => x.FirstName, out var fn))
            {
                //first name was set
            }

            if (patch.TryGetValue(x => x.Address.State.Abbreviation, out var abbvr))
            {
                //abbreviation was set
            }

            return Ok();
        }
    }
}

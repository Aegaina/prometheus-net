using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Tester.NetFramework.AspNet
{
    public class TestController : ApiController
    {
        // GET api/<controller>
        public IHttpActionResult Get()
        {
            return StatusCode(HttpStatusCode.MethodNotAllowed);
        }

        // GET api/<controller>/5
        public IHttpActionResult Get(int id)
        {
            return NotFound();
        }
    }
}
using DurableLockLibrary;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace LockTest
{
    [TestClass]
    public class UnitTest1
    {
        private static HttpClient HttpClient = new HttpClient();

        [TestMethod]
        public async Task TestMethod1()
        {
            await HttpClient.GetAsync("http://localhost:7071/clean");

            string lockType = "project";

            ////get locks input list from post data
            List<LockOperation> lockOps = new()
            {
                new LockOperation() { LockId = "a1", LockType = lockType },
                new LockOperation() { LockId = "a2", LockType = lockType, StayLocked = true },
                new LockOperation() { LockId = "a3", LockType = lockType }
            };

            var res = await HttpClient.PostAsJsonAsync("http://localhost:7071/Lock2/15", lockOps);
            var rese = await res.Content.ReadAsStringAsync();
            //List<string> result = JsonSerializer.Deserialize<List<string>>(await res.Content.ReadAsStringAsync());

            //foreach(var loc in result)
            //{
            //    res = await HttpClient.GetAsync(loc);
            //}
        }
    }
}
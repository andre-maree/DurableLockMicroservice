using Durable.Lock.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace LockTest
{
    [TestClass]
    public class UnitTest1
    {
        private static HttpClient HttpClient = new();

        [TestMethod]
        public async Task TestLockAndUnlockWithKey()
        {
            try
            {
                string lockType = "project";
                string lockName = "GenericLock";
                string key = "mykey123";

                //create lock input test data
                List<LockOperation> lockOps = new()
                {
                    new LockOperation() { LockId = "a1", LockType = lockType, User = "user1", LockName = lockName, Key = key }
                };

                //delete the lock to start from scratch
                HttpResponseMessage del1 = await HttpClient.GetAsync($"http://localhost:7071/DeleteLock/{lockName}/{lockType}/a1");

                if (del1.StatusCode == System.Net.HttpStatusCode.Accepted)
                {
                    while (true)
                    {
                        await Task.Delay(1000);

                        HttpResponseMessage readres = await HttpClient.GetAsync($"http://localhost:7071/ReadLock/{lockName}/{lockType}/a1");
                        var read = JsonSerializer.Deserialize<LockOperationResult>(await readres.Content.ReadAsStringAsync());

                        if (read?.User is null)
                        {
                            break;
                        }
                    }
                }
                else if (del1.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    Assert.Fail();
                }

                //lock it x2 to force a conflict //////////////////////////////////////////////////////
                var setLockResult = HttpClient.PostAsJsonAsync("http://localhost:7071/Lock/1000", lockOps);
                //var resLocks3 = HttpClient.PostAsJsonAsync("http://localhost:7071/Lock/1000", lockOps);

                LockResultResponse result = JsonSerializer.Deserialize<LockResultResponse>(await setLockResult.Result.Content.ReadAsStringAsync());

                //unlock the 3 locks, wait 150 seconds to ensure an 200 result and not a 202 ///////////
                lockOps[0].Key = null;
                var unLockResponse = await HttpClient.PostAsJsonAsync("http://localhost:7071/unLock/150", lockOps);

                LockResultResponse unLockResult = JsonSerializer.Deserialize<LockResultResponse>(await unLockResponse.Content.ReadAsStringAsync());

                //there should be no locks returned
                Assert.IsTrue(unLockResponse.StatusCode == System.Net.HttpStatusCode.OK && unLockResult.Locks.Count == 0);

                //delete should not work without the key
                var delfail = await HttpClient.GetAsync($"http://localhost:7071/DeleteLock/{lockName}/{lockType}/a1");

                Assert.IsTrue(delfail.StatusCode == System.Net.HttpStatusCode.OK || delfail.StatusCode == System.Net.HttpStatusCode.Accepted);

                //unlock with the key should work
                lockOps[0].Key = key;
                unLockResponse = await HttpClient.PostAsJsonAsync("http://localhost:7071/unLock/150", lockOps);

                unLockResult = JsonSerializer.Deserialize<LockResultResponse>(await unLockResponse.Content.ReadAsStringAsync());

                //there should be 1 lock returned unlocked
                Assert.IsTrue(unLockResponse.StatusCode == System.Net.HttpStatusCode.OK && unLockResult.Locks[0].IsLocked == false);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Run both tests in a loop
        /// </summary>
        [TestMethod]
        public async Task TestLoop()
        {
            try
            {
                for (int i = 0; i < 2; i++)
                {
                    await TestLockAndUnlockWithKey();
                    await TestConcurrency();
                    await TestAll();
                }

                Assert.IsTrue(true);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Test concurrence confliction by locking the same lock x2 concurrently
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task TestConcurrency()
        {
            try
            {
                string lockType = "project";
                string lockName = "GenericLock";

                //create lock input test data
                List<LockOperation> lockOps = new()
                {
                    new LockOperation() { LockId = "a1", LockType = lockType, User = "user1", LockName = lockName }//, Key = "mykey" }
                };

                //delete the lock to start from scratch
                HttpResponseMessage del1 = await HttpClient.GetAsync($"http://localhost:7071/DeleteLock/{lockName}/{lockType}/a1");

                if (del1.StatusCode == System.Net.HttpStatusCode.Accepted)
                {
                    while (true)
                    {
                        await Task.Delay(1000);

                        HttpResponseMessage readres = await HttpClient.GetAsync($"http://localhost:7071/ReadLock/{lockName}/{lockType}/a1");
                        var read = JsonSerializer.Deserialize<LockOperationResult>(await readres.Content.ReadAsStringAsync());

                        if (read?.User is null)
                        {
                            break;
                        }
                    }
                }
                else if (del1.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    Assert.Fail();
                }

                //lock it x2 to force a conflict //////////////////////////////////////////////////////
                var resLocks2 = HttpClient.PostAsJsonAsync("http://localhost:7071/Lock/1000", lockOps);
                var resLocks3 = HttpClient.PostAsJsonAsync("http://localhost:7071/Lock/1000", lockOps);

                LockResultResponse result = JsonSerializer.Deserialize<LockResultResponse>(await resLocks2.Result.Content.ReadAsStringAsync());

                LockResultResponse result2 = JsonSerializer.Deserialize<LockResultResponse>(await resLocks3.Result.Content.ReadAsStringAsync());

                //1 lock attempt should be successful and the other must be a conflict
                Assert.IsTrue((result.HttpStatusCode == 201 && result2.HttpStatusCode == 409) || (result.HttpStatusCode == 409 && result2.HttpStatusCode == 201));
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Test all locking functionality with 3 locks
        /// </summary>
        [TestMethod]
        public async Task TestAll()
        {
            try
            {
                string lockType = "project";
                string lockName = "GenericLock";

                //create 3 locks input test data
                List<LockOperation> lockOps = new()
                {
                    new LockOperation() { LockId = "a1", LockType = lockType, User = "user1", LockName = lockName },
                    new LockOperation() { LockId = "a2", LockType = lockType, User = "user2", LockName = lockName },
                    new LockOperation() { LockId = "a3", LockType = lockType, User = "user3", LockName = lockName }
                };

                //delete these 3 locks to start from scratch
                HttpResponseMessage del1 = await HttpClient.GetAsync($"http://localhost:7071/DeleteLock/{lockName}/{lockType}/a1");
                HttpResponseMessage del2 = await HttpClient.GetAsync($"http://localhost:7071/DeleteLock/{lockName}/{lockType}/a2");
                HttpResponseMessage del3 = await HttpClient.GetAsync($"http://localhost:7071/DeleteLock/{lockName}/{lockType}/a3");

                //check the http result of the 3 deletes, it can be a 200 or 201
                Assert.IsTrue(del1.StatusCode == System.Net.HttpStatusCode.OK || del1.StatusCode == System.Net.HttpStatusCode.Accepted);
                Assert.IsTrue(del2.StatusCode == System.Net.HttpStatusCode.OK || del2.StatusCode == System.Net.HttpStatusCode.Accepted);
                Assert.IsTrue(del3.StatusCode == System.Net.HttpStatusCode.OK || del3.StatusCode == System.Net.HttpStatusCode.Accepted);

                //poll to check that all 3 deletes were successful /////////////////////////////////////////////////////////////////////////////
                HttpResponseMessage readres = await HttpClient.PostAsJsonAsync("http://localhost:7071/ReadLocks", lockOps);
                var reads = JsonSerializer.Deserialize<List<LockOperationResult>>(await readres.Content.ReadAsStringAsync());

                while (reads.FindAll(l => l.User == null).Count < 3)
                {
                    readres = await HttpClient.PostAsJsonAsync("http://localhost:7071/ReadLocks", lockOps);
                    reads = JsonSerializer.Deserialize<List<LockOperationResult>>(await readres.Content.ReadAsStringAsync());

                    await Task.Delay(1000);
                }

                //if the user of all 3 locks is null, then it is deleted
                Assert.IsTrue(reads.FindAll(l => l.User == null).Count == 3);


                //lock all 3 locks, wait 150 seconds for a result, it should create the 3 locks in 150 seconds with a 201 result //////////////////
                HttpResponseMessage resLocks = await HttpClient.PostAsJsonAsync("http://localhost:7071/Lock/150", lockOps);
                LockResultResponse result = JsonSerializer.Deserialize<LockResultResponse>(await resLocks.Content.ReadAsStringAsync());

                Assert.IsTrue(result.HttpStatusCode == 201);
                Assert.IsTrue(result.Locks.FindAll(l => l.IsLocked == true
                                                        && (l.User.Equals("user1") || l.User.Equals("user2") || l.User.Equals("user3"))
                                                        && (l.LockId.Equals("a1") || l.LockId.Equals("a2") || l.LockId.Equals("a3"))).Count == 3);


                //lock all 3 again to create conflict 409 //////////////////////////////////////////////////////
                resLocks = await HttpClient.PostAsJsonAsync("http://localhost:7071/Lock/150", lockOps);
                result = JsonSerializer.Deserialize<LockResultResponse>(await resLocks.Content.ReadAsStringAsync());

                Assert.IsTrue(result.HttpStatusCode == 409);
                Assert.IsTrue(result.Locks.FindAll(l => l.IsLocked == true
                                                        && l.Conflicted == true
                                                        && (l.User.Equals("user1") || l.User.Equals("user2") || l.User.Equals("user3"))
                                                        && (l.LockId.Equals("a1") || l.LockId.Equals("a2") || l.LockId.Equals("a3"))).Count == 3);


                //now read the 3 locks, they must still be locked, no conflicts, user is set ///////////////////////
                readres = await HttpClient.PostAsJsonAsync("http://localhost:7071/ReadLocks", lockOps);
                reads = JsonSerializer.Deserialize<List<LockOperationResult>>(await readres.Content.ReadAsStringAsync());

                Assert.IsTrue(reads.FindAll(l => !string.IsNullOrWhiteSpace(l.User) && l.Conflicted == false && l.IsLocked).Count == 3);


                //unlock the 3 locks, wait 150 seconds to ensure an 200 result and not a 202 ///////////
                resLocks = await HttpClient.PostAsJsonAsync("http://localhost:7071/unLock/150", lockOps);
                result = JsonSerializer.Deserialize<LockResultResponse>(await resLocks.Content.ReadAsStringAsync());

                Assert.IsTrue(result.HttpStatusCode == 200);
                Assert.IsTrue(result.Locks.FindAll(l => l.IsLocked == false
                                                        && l.Conflicted == false
                                                        && (l.User.Equals("user1") || l.User.Equals("user2") || l.User.Equals("user3"))
                                                        && (l.LockId.Equals("a1") || l.LockId.Equals("a2") || l.LockId.Equals("a3"))).Count == 3);


                //now read locks again after the unlock ///////////////////////////////////////////////
                readres = await HttpClient.PostAsJsonAsync("http://localhost:7071/ReadLocks", lockOps);
                reads = JsonSerializer.Deserialize<List<LockOperationResult>>(await readres.Content.ReadAsStringAsync());

                Assert.IsTrue(reads.FindAll(l => !string.IsNullOrWhiteSpace(l.User) && l.Conflicted == false && !l.IsLocked).Count == 3);


                //lock just the a1 lock, ensure 150 second wait for a 201 and not a 202 ///////////////////////////////////////////////////
                resLocks = await HttpClient.PostAsJsonAsync("http://localhost:7071/Lock/150", lockOps.FindAll(l => l.LockId.Equals("a1")));
                result = JsonSerializer.Deserialize<LockResultResponse>(await resLocks.Content.ReadAsStringAsync());

                Assert.IsTrue(result.HttpStatusCode == 201);
                Assert.IsTrue(result.Locks.FindAll(l => l.IsLocked == true
                                                        && l.User.Equals("user1")
                                                        && l.LockId.Equals("a1")).Count == 1);


                //lock all to create conflict for the a1 lock, a2 and a3 stays unlocked beacuse a1 conflicted and is passed as 1 batch together ////
                resLocks = await HttpClient.PostAsJsonAsync("http://localhost:7071/Lock/150", lockOps);
                result = JsonSerializer.Deserialize<LockResultResponse>(await resLocks.Content.ReadAsStringAsync());

                Assert.IsTrue(result.HttpStatusCode == 409);
                Assert.IsTrue(result.Locks.FindAll(l => l.IsLocked == true
                                                        && l.Conflicted
                                                        && l.User.Equals("user1")
                                                        && l.LockId.Equals("a1")).Count == 1);

                Assert.IsTrue(result.Locks.FindAll(l => l.IsLocked == false
                                                        && l.Conflicted == false
                                                        && (l.User.Equals("user2") || l.User.Equals("user3"))
                                                        && (l.LockId.Equals("a2") || l.LockId.Equals("a3"))).Count == 2);


                //read the locks to check that a1 is locked and a2 and a3 is unlocked //////////////////////////
                readres = await HttpClient.PostAsJsonAsync("http://localhost:7071/ReadLocks", lockOps);
                reads = JsonSerializer.Deserialize<List<LockOperationResult>>(await readres.Content.ReadAsStringAsync());

                Assert.IsTrue(reads.FindAll(l => !string.IsNullOrWhiteSpace(l.User)).Count == 3);
                Assert.IsTrue(reads.FindAll(l => l.LockId.Equals("a1") && l.IsLocked).Count == 1);
                Assert.IsTrue(reads.FindAll(l => !l.LockId.Equals("a1") && !l.IsLocked).Count == 2);

                //delete the 3 locks //////////////////////////////////////////////////////////////////////////
                del1 = await HttpClient.GetAsync($"http://localhost:7071/DeleteLock/{lockName}/{lockType}/a1");
                del2 = await HttpClient.GetAsync($"http://localhost:7071/DeleteLock/{lockName}/{lockType}/a2");
                del3 = await HttpClient.GetAsync($"http://localhost:7071/DeleteLock/{lockName}/{lockType}/a3");

                Assert.IsTrue(del1.StatusCode == System.Net.HttpStatusCode.OK || del1.StatusCode == System.Net.HttpStatusCode.Accepted);
                Assert.IsTrue(del2.StatusCode == System.Net.HttpStatusCode.OK || del2.StatusCode == System.Net.HttpStatusCode.Accepted);
                Assert.IsTrue(del3.StatusCode == System.Net.HttpStatusCode.OK || del3.StatusCode == System.Net.HttpStatusCode.Accepted);


                //poll to check that all 3 deletes were successful /////////////////////////////////////////////////////////////////////////////
                readres = await HttpClient.PostAsJsonAsync("http://localhost:7071/ReadLocks", lockOps);
                reads = JsonSerializer.Deserialize<List<LockOperationResult>>(await readres.Content.ReadAsStringAsync());

                while (reads.FindAll(l => l.User == null).Count < 3)
                {
                    readres = await HttpClient.PostAsJsonAsync("http://localhost:7071/ReadLocks", lockOps);
                    reads = JsonSerializer.Deserialize<List<LockOperationResult>>(await readres.Content.ReadAsStringAsync());

                    await Task.Delay(1000);
                }

                //if the user of all 3 locks is null, then it is deleted
                Assert.IsTrue(reads.FindAll(l => l.User == null).Count == 3);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
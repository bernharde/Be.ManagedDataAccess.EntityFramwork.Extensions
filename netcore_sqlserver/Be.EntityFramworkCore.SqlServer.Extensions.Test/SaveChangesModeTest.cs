﻿using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Be.EntityFrameworkCore.SqlServer.Test
{
    [TestClass]
    public class SaveChangesModeTest
    {
        [TestMethod]
        [ExpectedException(typeof(DbUpdateException))]
        public void NotIgnoreEntityDublicateKeyError()
        {
            var id = Guid.NewGuid();

            // create first entity with id
            using (var cx = LastWins.CreateContext())
            {
                var lw = new LastWinsEntity();
                lw.Id = id;
                lw.Name = "v1";
                lw.Created = lw.Updated = DateTime.Now;
                cx.LastWins.Add(lw);
                cx.SaveChanges(); // save first one
            }

            // create second entity with same id -> will throw a DbUpdateException
            using (var cx = LastWins.CreateContext())
            {
                var lw2 = new LastWinsEntity();
                lw2.Id = id;
                lw2.Name = "v1";
                lw2.Created = lw2.Updated = DateTime.Now;
                cx.LastWins.Add(lw2);
                cx.SaveChanges();
            }
        }
        [TestMethod]
        public void IgnoreEntityDublicateKeyError()
        {
            var id = Guid.NewGuid();

            // create first entity with id (primary key)
            using (var cx = LastWins.CreateContext())
            {
                var lw = new LastWinsEntity();
                lw.Id = id;
                lw.Name = "v1";
                lw.Created = lw.Updated = DateTime.Now;
                cx.LastWins.Add(lw);
                cx.SaveChanges(); // save first one
            }

            // create second entity with same id (primary key) 
            // -> Dublicate key error will be ignored, 
            // -> second entity will not be added to db
            using (var cx = LastWins.CreateContext())
            {
                var lw2 = new LastWinsEntity();
                lw2.Id = id;
                lw2.Name = "v1";
                lw2.Created = lw2.Updated = DateTime.Now;
                cx.LastWins.Add(lw2);
                cx.SaveChanges(SaveChangesMode.IgnoreEntityDublicateKey);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(DbUpdateConcurrencyException))]
        public void NotIgnoreEntityDeleted()
        {
            var id = Guid.NewGuid();
            using (var cx = LastWins.CreateContext())
            {
                var lw = new LastWinsEntity();
                lw.Id = id;
                lw.Name = "v1";
                lw.Created = lw.Updated = DateTime.Now;
                cx.LastWins.Add(lw);
                cx.SaveChanges(); // save first one

                Delete(id);

                lw.Name = "v2";
                lw.Updated = DateTime.Now;
                cx.SaveChanges();
            }
        }

        [TestMethod]
        public void IgnoreEntityDeleted2()
        {
            var id = Guid.NewGuid();
            using (var cx = LastWins.CreateContext())
            {
                var lw = new LastWinsEntity();
                lw.Id = id;
                lw.Name = "v1";
                lw.Created = lw.Updated = DateTime.Now;
                cx.LastWins.Add(lw);
                cx.SaveChanges(); // save first one

                Delete(id);

                cx.LastWins.Remove(lw); // already deleted
                cx.SaveChanges(SaveChangesMode.IgnoreEntityDeleted);
            }
        }

        [TestMethod]
        public void IgnoreEntityDeleted()
        {
            var id = Guid.NewGuid();
            using (var cx = LastWins.CreateContext())
            {
                var lw = new LastWinsEntity();
                lw.Id = id;
                lw.Name = "v1";
                lw.Created = lw.Updated = DateTime.Now;
                cx.LastWins.Add(lw);
                cx.SaveChanges(); // save first one

                Delete(id);

                lw.Name = "v2";
                lw.Updated = DateTime.Now;
                cx.SaveChanges(SaveChangesMode.IgnoreEntityDeleted);
            }
        }

        [TestMethod]
        public void IgnoreEntityDeletedAndIgnoreEntityDublicateKeyError()
        {
            var id = Guid.NewGuid();
            var id2 = Guid.NewGuid();

            using (var cx = LastWins.CreateContext())
            {
                var lw2 = new LastWinsEntity();
                lw2.Id = id2;
                lw2.Name = "v1";
                lw2.Created = lw2.Updated = DateTime.Now;
                cx.LastWins.Add(lw2);
                cx.SaveChanges();
            }

            using (var cx = LastWins.CreateContext())
            {
                var lw = new LastWinsEntity();
                lw.Id = id;
                lw.Name = "v1";
                lw.Created = lw.Updated = DateTime.Now;
                cx.LastWins.Add(lw);
                cx.SaveChanges();

                Delete(id); // meantime delete lw1

                lw.Name = "v2"; // modify lw1
                lw.Updated = DateTime.Now;

                var lw3 = new LastWinsEntity(); // lw3 dublicate of lw2
                lw3.Id = id2;
                lw3.Name = "v1";
                lw3.Created = lw3.Updated = DateTime.Now;
                cx.LastWins.Add(lw3);

                cx.SaveChanges(SaveChangesMode.All);
            }
        }

        private void Delete(Guid id)
        {
            Task.Run(() =>
            {
                using (var cx = LastWins.CreateContext())
                {
                    var lw = cx.LastWins.FirstOrDefault(e => e.Id == id);
                    cx.LastWins.Remove(lw);
                    cx.SaveChanges();
                }
            }).Wait();
        }
    }
}

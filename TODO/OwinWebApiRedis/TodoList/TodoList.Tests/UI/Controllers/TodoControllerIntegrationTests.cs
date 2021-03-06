﻿using System;
using System.Net;
using System.Linq;
using System.Text;
using System.Net.Http;
using TodoList.Console;
using ServiceStack.Redis;
using Microsoft.Owin.Testing;
using System.Collections.Generic;
using TodoList.Console.Domain.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TodoList.Console.Infrastructure.Data.Repositories;
using TodoList.Console.UI.Models;

namespace TodoList.Tests.UI.Controllers
{
    [TestClass]
    public class TodoControllerIntegrationTests
    {
        private TestServer _server;
        private TasksRepository _repository;
        private Task _taskOne, _taskTwo;

        [TestInitialize]
        public void Initialize()
        {
            _server = TestServer.Create<StartUp>();
            _repository = new TasksRepository(new RedisClient());
            _taskOne = _repository.Save(new Task { Title = "TaskOne" });
            _taskTwo = _repository.Save(new Task { Title = "TaskTwo" });
        }

        [TestCleanup]
        public void CleanUp()
        {
            _repository.Delete(_taskOne.Id);
            _repository.Delete(_taskTwo.Id);
        }

        [TestMethod]
        public void TodoController_Get()
        {
            var response = _server.HttpClient.GetAsync("/todo").Result;
            var result = response.Content.ReadAsStringAsync().Result;

            var tasks = JsonHelper.JsonDeserialize<IList<TaskModel>>(result);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.IsTrue(tasks.ToList().Count >= 2);
        }

        [TestMethod]
        public void TodoController_Get_by_id()
        {
            var response = _server.HttpClient.GetAsync(string.Format("/todo/{0}", _taskTwo.Id)).Result;
            var result = response.Content.ReadAsStringAsync().Result;
            var taskFound = JsonHelper.JsonDeserialize<TaskModel>(result);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(taskFound.id, _taskTwo.Id);
        }

        [TestMethod]
        public void TodoController_Post()
        {
            var jsonTask = JsonHelper.JsonSerialize<Task>(new Task { Title = "Task to Post"});

            var content = new System.Net.Http.StringContent(jsonTask, Encoding.UTF8, "application/json");
            var response = _server.HttpClient.PostAsync("/todo", content).Result;
            var result = response.Content.ReadAsStringAsync().Result;
            var taskFound = JsonHelper.JsonDeserialize<TaskModel>(result);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreNotEqual(Guid.Empty, taskFound.id);

            _repository.Delete(taskFound.id);
        }

        [TestMethod]
        public void TodoController_Put()
        {
            _taskOne.Title = "Name Updated";
            var jsonTask = JsonHelper.JsonSerialize<Task>(_taskOne);

            var content = new System.Net.Http.StringContent(jsonTask, Encoding.UTF8, "application/json");
            var response = _server.HttpClient.PutAsync("/todo", content).Result;
            var result = response.Content.ReadAsStringAsync().Result;
            var taskUpdated = JsonHelper.JsonDeserialize<TaskModel>(result);
            var taskFound = _repository.GetById(_taskOne.Id);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(taskUpdated.id, taskFound.Id);
            Assert.AreEqual(taskUpdated.title, taskFound.Title);

            _repository.Delete(taskFound.Id);
        }

        [TestMethod]
        public void TodoController_Delete()
        {
            var jsonTask = JsonHelper.JsonSerialize<Task>(_taskOne);

            var content = new System.Net.Http.StringContent(jsonTask, Encoding.UTF8, "application/json");
            var response = _server.HttpClient.DeleteAsync(string.Format("/todo/{0}", _taskOne.Id)).Result;
            var taskFound = _repository.GetById(_taskOne.Id);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.IsNull(taskFound);
        }
    }
}

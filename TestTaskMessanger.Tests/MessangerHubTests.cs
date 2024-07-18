using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using TestTaskMessanger.Dbl.Data;
using TestTaskMessanger.Dbl.Data.Entities;
using TestTaskMessanger.Dbl.Repository;
using TestTaskMessanger.DTO;
using TestTaskMessanger.Hubs;
using TestTaskMessanger.Models;
using TestTaskMessanger.Utils;

namespace TestTaskMessanger.Tests
{
    public class MessangerHubTests
    {
        private readonly HubConnection _hubConnection;
        private readonly MessangerDbContext _dbContext;
        private readonly MessangerRepository _repository;
        private readonly MessangerHub _hub;

        private readonly Mock<MesMemoryCache> _mockCache;
        private readonly Mock<ILogger<MessangerHub>> _mockLogger;
        private readonly Mock<IHubCallerClients<IMessangerHub>> _mockClients;
        private readonly Mock<IMessangerHub> _mockCaller;
        private readonly Mock<IGroupManager> _mockGroups;
        private readonly Mock<HubCallerContext> _mockHubContext;

        public MessangerHubTests()
        {
            var options = new DbContextOptionsBuilder<MessangerDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _hubConnection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5151/Messanger")
                .Build();

            _dbContext = new MessangerDbContext(options);
            _dbContext.Database.EnsureCreated();
            if (_dbContext.Users.Count() <= 0)
            {
                for (int i = 0; i < 10; i++)
                {
                    _dbContext.Add(new UserEntity
                    {
                        Username = $"User-{i}",
                        Password = Cipner.Encode($"Pass-{i}")
                    });
                }
            }

            if (_dbContext.Chats.Count() <= 0)
            {
                for (int i = 0; i < 10; i++)
                {
                    _dbContext.Add(new ChatEntity
                    {
                        ChatName = $"Chat-{i}",
                        Admin = _dbContext.Users.Where(user => user.Username == $"User-{i}").FirstOrDefault(),
                    });
                }
                _dbContext.SaveChanges();
            }

            _repository = new MessangerRepository(_dbContext);

            _mockCache = new Mock<MesMemoryCache>(new MemoryCache(new MemoryCacheOptions()));
            _mockLogger = new Mock<ILogger<MessangerHub>>();
            _mockCaller = new Mock<IMessangerHub>();
            _mockGroups = new Mock<IGroupManager>();
            _mockClients = new Mock<IHubCallerClients<IMessangerHub>>();
            _mockClients.Setup(clients => clients.Caller).Returns(_mockCaller.Object);
            _mockClients.Setup(clients => clients.Group(It.IsAny<string>())).Returns(_mockCaller.Object);

            _mockHubContext = new Mock<HubCallerContext>();
            _mockHubContext.Setup(context => context.ConnectionId).Returns("testConnectionId");

            
            

            _hub = new MessangerHub(_mockCache.Object, _mockLogger.Object, _repository);
            _hub.Groups = _mockGroups.Object;
            _hub.Context = _mockHubContext.Object;    
            _hub.Clients = _mockClients.Object;
        }

        [Fact]
        public async void CreateNewUserReturnsSuccessAddNewUser()
        {
            // Arrange
            await _hubConnection.StartAsync();
            var userModel = new UserModel { Username = "UniqUser", Password = "Pass" };

            // Act
            await _hub.CreateUser(userModel);

            // Assert
            _mockCaller.Verify(caller => caller.ReceiveMessage(It.IsAny<string>(), $"Hello in my app, [{userModel.Username}]"), Times.Once);
            await _hubConnection.StopAsync();
        }

        [Fact]
        public async void CreateNewUserReturnsErrorUserExists()
        {
            // Arrange
            await _hubConnection.StartAsync();
            _dbContext.Add(new UserEntity { Username = "ExistsUser" });
            _dbContext.SaveChanges();
            var userModel = new UserModel { Username = "ExistsUser", Password = "Pass"};

            // Act
            await _hub.CreateUser(userModel);

            // Assert
            _mockCaller.Verify(caller => caller.ReceiveMessage(It.IsAny<string>(), $"User with name: [{userModel.Username}] already exist"), Times.Once);
            await _hubConnection.StopAsync();
        }

        [Fact]
        public async Task JoinChatReturnsSuccessAddsUserToChat()
        {
            // Arrange
            await _hubConnection.StartAsync();
            UserConnectionModel userConnectionModel = new UserConnectionModel { Username = "User", Password = "pass", Chat = "testChat" };

            _dbContext.Add(new ChatEntity { ChatName = userConnectionModel.Chat });
            _dbContext.Add(new UserEntity { Username = userConnectionModel.Username, Password = Cipner.Encode(userConnectionModel.Password) });
            await _dbContext.SaveChangesAsync();
            
            // Act
            await _hub.JoinChat(userConnectionModel);

            // Assert
            _mockGroups.Verify(groups => groups.AddToGroupAsync(_hub.Context.ConnectionId, userConnectionModel.Chat, It.IsAny<CancellationToken>()), Times.Once);
            Assert.True(_mockCache.Object.TryGetValue(_hub.Context.ConnectionId, out string? existsChat));
            Assert.NotNull(existsChat);
            _mockClients.Verify(clients => clients.Group(existsChat).ReceiveMessage(It.IsAny<string>(), $"[{userConnectionModel.Username}] has been joined to chat: [{existsChat}]"));
            await _hubConnection.StopAsync();
        }

        [Fact]
        public async Task JoinChatReturnsErrorAlreadyInChat()
        {
            // Arrange
            await _hubConnection.StartAsync();
            UserConnectionModel userConnectionModel = new UserConnectionModel { Username = "User", Password = "pass", Chat = "testChat" };

            _dbContext.Add(new ChatEntity { ChatName = userConnectionModel.Chat });
            _dbContext.Add(new UserEntity { Username = userConnectionModel.Username, Password = Cipner.Encode(userConnectionModel.Password) });
            await _dbContext.SaveChangesAsync();

            // Act
            await _hub.JoinChat(userConnectionModel);
            await _hub.JoinChat(userConnectionModel);

            // Assert
            Assert.True(_mockCache.Object.TryGetValue(_hub.Context.ConnectionId, out string? oldChat));
            Assert.NotNull(oldChat);
            _mockClients.Verify(clients => clients.Caller.ReceiveMessage(It.IsAny<string>(), $"You already in chat: [{oldChat}]"));
            await _hubConnection.StopAsync();
        }

        [Fact]
        public async Task JoinChatReturnsErrorUserNotFound()
        {
            // Arrange
            await _hubConnection.StartAsync();
            UserConnectionModel userConnectionModel = new UserConnectionModel { Username = "User", Password = "pass", Chat = "testChat" };

            _dbContext.Add(new ChatEntity { ChatName = userConnectionModel.Chat });
            await _dbContext.SaveChangesAsync();

            // Act
            await _hub.JoinChat(userConnectionModel);

            // Assert
            _mockCaller.Verify(caller => caller.ReceiveMessage(It.IsAny<string>(), $"User [{userConnectionModel.Username}] not found"), Times.Once);
            await _hubConnection.StopAsync();
        }

        [Fact]
        public async Task JoinChatReturnsErrorChatNotFound()
        {
            // Arrange
            await _hubConnection.StartAsync();
            UserConnectionModel userConnectionModel = new UserConnectionModel { Username = "User", Password = "pass", Chat = "testChat" };

            _dbContext.Add(new UserEntity { Username = userConnectionModel.Username, Password = Cipner.Encode(userConnectionModel.Password) });
            await _dbContext.SaveChangesAsync();

            // Act
            await _hub.JoinChat(userConnectionModel);

            // Assert
            _mockCaller.Verify(caller => caller.ReceiveMessage(It.IsAny<string>(), $"Chat [{userConnectionModel.Chat}] not found"), Times.Once);
            await _hubConnection.StopAsync();
        }
    }
}
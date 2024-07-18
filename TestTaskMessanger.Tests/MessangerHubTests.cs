using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TestTaskMessanger.Dbl.Data;
using TestTaskMessanger.Dbl.Data.Entities;
using TestTaskMessanger.Dbl.Repository;
using TestTaskMessanger.Hubs;
using TestTaskMessanger.Models;
using TestTaskMessanger.Utils;

namespace TestTaskMessanger.Tests
{
    public class MessangerHubTests
    {
        
        private readonly MessangerDbContext _dbContext;
        private readonly MessangerRepository _repository;
        private readonly IMemoryCache _cache;
        private readonly MessangerHub _hub;

        private readonly Mock<ILogger<MessangerHub>> _logger;
        private readonly Mock<IHubCallerClients<IMessangerHub>> _mockClients;
        private readonly Mock<IMessangerHub> _mockCaller;

        public MessangerHubTests()
        {
            var options = new DbContextOptionsBuilder<MessangerDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

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
                _dbContext.SaveChanges();
            }
            _cache = new MesMemoryCache(new MemoryCache(new MemoryCacheOptions()));
            _logger = new Mock<ILogger<MessangerHub>>();
            _repository = new MessangerRepository(_dbContext);
            
            _mockClients = new Mock<IHubCallerClients<IMessangerHub>>();
            _mockCaller = new Mock<IMessangerHub>();
            _mockClients.Setup(clients => clients.Caller).Returns(_mockCaller.Object);

            _hub = new MessangerHub((MesMemoryCache)_cache, _logger.Object, _repository);
            _hub.Clients = _mockClients.Object;
        }

        [Fact]
        public async void CreateNewUserReturnsAddNewUser()
        {
            // Arrange
            var userModel = new UserModel { Username = "UniqUser", Password = "Pass" };
            
            // Act
            await _hub.CreateUser(userModel);

            // Assert
            _mockCaller.Verify(caller => caller.ReceiveMessage(It.IsAny<string>(), $"Hello in my app, [{userModel.Username}]"), Times.Once);

        }

        [Fact]
        public async void CreateNewUserReturnsUserExists()
        {
            // Arrange
            _dbContext.Add(new UserEntity { Username = "ExistsUser" });
            _dbContext.SaveChanges();
            var userModel = new UserModel { Username = "ExistsUser", Password = "Pass" };

            // Act
            await _hub.CreateUser(userModel);

            // Assert
            _mockCaller.Verify(caller => caller.ReceiveMessage(It.IsAny<string>(), $"User with name: [{userModel.Username}] already exist"), Times.Once);

        }
    }
}
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Funchey
{
    public class Item
    {
        public int MyProperty { get; set; }
    }
    public interface ICache
    {
        Task<Item> ResolveAsync(string cacheKey, Func<Task<Item>> func);
    }
    public class SomeOtherExecutor
    {
        public async virtual Task<Item> GetItem()
        {
            await Task.Delay(1000);

            return new Item() { MyProperty = 5 };
        }
    }
    public class Executor
    {
        private readonly ICache cache;
        private readonly SomeOtherExecutor someOther;

        public Executor(
            ICache cache,
            SomeOtherExecutor someOther)
        {
            this.cache = cache;
            this.someOther = someOther;
        }

        public async Task<Item> GetItem()
        {
            try
            {
                var result = await this.cache.ResolveAsync("", async () => await someOther.GetItem());

                return result;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
    public class UnitTest
    {
        private const string Message = "damn";
        private Mock<ICache> cacheMock = new Mock<ICache>();
        private SomeOtherExecutor someOtherExecutor = new SomeOtherExecutor();

        [Fact]
        public async Task Returns()
        {
            Item response = null;

            this.cacheMock
                .Setup(t => t.ResolveAsync(It.IsAny<string>(), It.IsAny<Func<Task<Item>>>()))
                .Returns(async (string key, Func<Task<Item>> func) => 
                { return await func(); });    
            var target = new Executor(this.cacheMock.Object, this.someOtherExecutor);

            var item = await target.GetItem();

            Assert.Equal(5, item.MyProperty);
        }

        [Fact]
        public async Task Throws()
        {
            var someOtherExecutor = new Mock<SomeOtherExecutor>();

            someOtherExecutor
                .Setup(t => t.GetItem())
                .ThrowsAsync(new AccessViolationException(Message));
            this.cacheMock
                .Setup(t => t.ResolveAsync(It.IsAny<string>(), It.IsAny<Func<Task<Item>>>()))
                .Returns(async (string key, Func<Task<Item>> func) =>
                { return await func(); });

            var target = new Executor(this.cacheMock.Object, someOtherExecutor.Object);

            await Assert.ThrowsAsync<AccessViolationException>(async () => await target.GetItem());
        }
    }
}
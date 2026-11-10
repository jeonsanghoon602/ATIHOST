using System.Collections.Concurrent;
using System.Threading.Tasks;
using HostSimulator.Protocol;
using Xunit;

namespace HostSimulator.Tests
{
    public class SystemByteGeneratorTests
    {
        [Fact]
        public void Next_increments()
        {
            var gen = new SystemByteGenerator();
            var first = gen.Next();
            var second = gen.Next();
            Assert.Equal(first + 1, second);
        }

        [Fact]
        public void Initial_value_is_respected()
        {
            var gen = new SystemByteGenerator(99);
            Assert.Equal(100u, gen.Next());
        }

        [Fact]
        public void Wraps_on_overflow()
        {
            var gen = new SystemByteGenerator(uint.MaxValue);
            var value = gen.Next();
            Assert.Equal(0u, value);
        }

        [Fact]
        public void Thread_safe_no_duplicates()
        {
            var gen = new SystemByteGenerator();
            var values = new ConcurrentBag<uint>();
            var tasks = new Task[100];

            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    for (int j = 0; j < 100; j++)
                        values.Add(gen.Next());
                });
            }

            Task.WaitAll(tasks);

            var set = new System.Collections.Generic.HashSet<uint>(values);
            Assert.Equal(10000, set.Count);
        }
    }
}

namespace LogService.Application.Abstractions.Messages;
using System.Threading.Tasks;

public interface ILogMessageSeeder
{
    Task SeedAsync();
}

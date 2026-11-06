using Benday.MoqDemo.Business;
using Benday.MoqDemo.DataAccess;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json")
    .Build();

PersonDbContext.ConnectionString = config.GetConnectionString("MoqDemo")
    ?? throw new InvalidOperationException("Missing connection string 'MoqDemo'.");

using (var context = new PersonDbContext())
{
    context.Database.EnsureCreated();
}

var manager = new PersonManager(
    new SqlPersonRepository(new PersonDbContext()), new Logger(), TimeProvider.System);
Person? current = null;

while (true)
{
    Console.WriteLine();
    Console.Write("(N)ew, (L)oad, (E)dit notes and save, (Q)uit: ");
    var choice = Console.ReadLine()?.Trim().ToUpperInvariant();

    if (choice == "N")
    {
        current = manager.CreateNew();
        current.FirstName = Prompt("First name");
        current.LastName = Prompt("Last name");
        current.EmailAddress = Prompt("Email");
        await manager.SaveAsync(current);
        Show(current);
    }
    else if (choice == "L")
    {
        current = await manager.LoadByIdAsync(int.Parse(Prompt("Id")));
        Show(current);
    }
    else if (choice == "E" && current != null)
    {
        current.Notes = Prompt("Notes");
        await manager.SaveAsync(current);
        Show(current);
    }
    else if (choice == "Q" || choice == null)
    {
        break;
    }
}

static string Prompt(string label)
{
    Console.Write($"{label}: ");
    return Console.ReadLine() ?? string.Empty;
}

static void Show(Person? person)
{
    if (person == null)
    {
        Console.WriteLine("Not found.");
        return;
    }

    Console.WriteLine($"#{person.Id} {person.FirstName} {person.LastName} <{person.EmailAddress}>");
    Console.WriteLine($"  Notes: {person.Notes}");
    Console.WriteLine($"  Created {person.CreateDate:u}, last modified {person.LastModified:u}");
}

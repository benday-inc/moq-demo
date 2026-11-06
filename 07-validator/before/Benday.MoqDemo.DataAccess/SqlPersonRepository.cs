using Benday.EfCore.Repositories;
using Benday.MoqDemo.Common;
using Microsoft.EntityFrameworkCore;

namespace Benday.MoqDemo.DataAccess;

public class SqlPersonRepository : EfCoreCrudRepositoryBase<PersonEntity, PersonDbContext>
{
    public SqlPersonRepository(PersonDbContext context) : base(context)
    {
    }

    protected override DbSet<PersonEntity> EntityDbSet => Context.Persons;
}

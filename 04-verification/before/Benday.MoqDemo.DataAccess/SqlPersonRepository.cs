using Benday.EfCore.Repositories;
using Benday.MoqDemo.Common;
using Microsoft.EntityFrameworkCore;

namespace Benday.MoqDemo.DataAccess;

// The real repository. EfCoreCrudRepositoryBase (from Benday.EfCore) supplies
// GetByIdAsync, GetAllAsync, SaveAsync and DeleteAsync; this class only says
// which DbSet to use. It implements IAsyncReadableRepository<PersonEntity, int>,
// the interface PersonManager depends on.
public class SqlPersonRepository : EfCoreCrudRepositoryBase<PersonEntity, PersonDbContext>
{
    public SqlPersonRepository(PersonDbContext context) : base(context)
    {
    }

    protected override DbSet<PersonEntity> EntityDbSet => Context.Persons;
}

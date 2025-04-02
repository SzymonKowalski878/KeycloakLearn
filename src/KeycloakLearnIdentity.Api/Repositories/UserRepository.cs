using Feree.ResultType.Results;
using Feree.ResultType;
using KeycloakLearnIdentity.Api.Database;
using KeycloakLearnIdentity.Api.Models;
using Microsoft.EntityFrameworkCore;
using Feree.ResultType.Factories;

namespace KeycloakLearnIdentity.Api.Repositories;

public interface IUserRepository
{
    Task<IResult<User>> GetUserByIdAsync(Guid id);
    Task<IResult<User>> GetUserByKeycloakIdAsync(string keycloakId);
    Task<IResult<User>> GetUserByConfirmationTokenAsync(string token);
    Task<IResult<User>> AddUserAsync(User user);
    Task<IResult<User>> UpdateUserAsync(User user);
    Task<IResult<Unit>> DeleteUserAsync(User user);
    Task<IResult<List<User>>> GetAllUsersAsync();
    Task<IResult<Unit>> CommitAsync();
}

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _dbContext;

    public UserRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IResult<User>> GetUserByIdAsync(Guid id)
    {
        var user = await _dbContext.Users.FindAsync(id);
        return user != null ? ResultFactory.CreateSuccess(user) : ResultFactory.CreateFailure<User>("User not found.");
    }

    public async Task<IResult<User>> GetUserByKeycloakIdAsync(string keycloakId)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.KeycloakId == keycloakId);
        return user != null ? ResultFactory.CreateSuccess(user) : ResultFactory.CreateFailure<User>("User not found.");
    }

    public async Task<IResult<User>> GetUserByConfirmationTokenAsync(string token)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.ConfirmationToken == token);
        return user != null ? ResultFactory.CreateSuccess(user) : ResultFactory.CreateFailure<User>("Invalid confirmation token.");
    }

    public Task<IResult<User>> AddUserAsync(User user)
    {
        _dbContext.Users.Add(user);
        return ResultFactory.CreateSuccessAsync(user);
    }

    public Task<IResult<User>> UpdateUserAsync(User user)
    {
        _dbContext.Users.Update(user);
        return ResultFactory.CreateSuccessAsync(user);
    }

    public Task<IResult<Unit>> DeleteUserAsync(User user)
    {
        _dbContext.Users.Remove(user);
        return ResultFactory.CreateSuccessAsync();
    }

    public async Task<IResult<List<User>>> GetAllUsersAsync()
    {
        var users = await _dbContext.Users.ToListAsync();
        return ResultFactory.CreateSuccess(users);
    }

    public async Task<IResult<Unit>> CommitAsync()
    {
        await _dbContext.SaveChangesAsync();
        return ResultFactory.CreateSuccess();
    }
}
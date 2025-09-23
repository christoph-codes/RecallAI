using Microsoft.EntityFrameworkCore;
using RecallAI.Api.Data;
using RecallAI.Api.Interfaces;
using RecallAI.Api.Models;

namespace RecallAI.Api.Repositories;

public class MemoryRepository : IMemoryRepository
{
    private readonly MemoryDbContext _context;

    public MemoryRepository(MemoryDbContext context)
    {
        _context = context;
    }

    public async Task<Memory?> GetByIdAsync(Guid id, Guid userId)
    {
        return await _context.Memories
            .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);
    }

    public async Task<List<Memory>> GetAllByUserAsync(Guid userId, int page, int pageSize)
    {
        return await _context.Memories
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetCountByUserAsync(Guid userId)
    {
        return await _context.Memories
            .CountAsync(m => m.UserId == userId);
    }

    public async Task<Memory> CreateAsync(Memory memory)
    {
        memory.Id = Guid.NewGuid();
        memory.CreatedAt = DateTimeOffset.UtcNow;
        memory.UpdatedAt = DateTimeOffset.UtcNow;

        _context.Memories.Add(memory);
        await _context.SaveChangesAsync();
        return memory;
    }

    public async Task<Memory> UpdateAsync(Memory memory)
    {
        memory.UpdatedAt = DateTimeOffset.UtcNow;
        _context.Memories.Update(memory);
        await _context.SaveChangesAsync();
        return memory;
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId)
    {
        var memory = await _context.Memories
            .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);

        if (memory == null)
            return false;

        _context.Memories.Remove(memory);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(Guid id, Guid userId)
    {
        return await _context.Memories
            .AnyAsync(m => m.Id == id && m.UserId == userId);
    }
}
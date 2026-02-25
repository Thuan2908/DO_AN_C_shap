using System;
using System.Collections.Generic;
using System.Text;
using SQLite;
using FoodStreet.Models;

namespace FoodStreet.Models;

public class PoiRepository
{
    SQLiteAsyncConnection db;

    public async Task Init()
    {
        if (db != null)
            return;

        var path = Path.Combine(FileSystem.AppDataDirectory, "poi.db");

        db = new SQLiteAsyncConnection(path);

        await db.CreateTableAsync<Poi>();
    }

    public Task<List<Poi>> GetAll()
        => db.Table<Poi>().ToListAsync();

    public Task<int> Insert(Poi poi)
        => db.InsertAsync(poi);
}


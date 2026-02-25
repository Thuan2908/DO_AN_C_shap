using System;
using System.Collections.Generic;
using System.Text;
using SQLite;

namespace FoodStreet.Models;

public class Poi
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public double Radius { get; set; }
    public int Priority { get; set; }

    public string Description { get; set; }

    public string TtsScript { get; set; }
}


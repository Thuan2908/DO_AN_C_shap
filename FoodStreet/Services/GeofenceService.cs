//using FoodStreet.Models;
//namespace FoodStreet.Services;

//public class GeofenceService
//{
//    public Poi? CheckNearby(Location userLocation, List<Poi> pois)
//    {
//        foreach (var poi in pois)
//        {
//            var distanceKm = Location.CalculateDistance(
//                userLocation,
//                new Location(poi.Latitude, poi.Longitude),
//                DistanceUnits.Kilometers);

//            var distanceMeters = distanceKm * 1000;
//            Console.WriteLine($"User: {userLocation.Latitude}, {userLocation.Longitude}");
//            Console.WriteLine($"POI: {poi.Latitude}, {poi.Longitude}");
//            Console.WriteLine($"khoảng cách: {distanceMeters} m");

//            if (distanceMeters <= poi.Radius)
//                return poi;
//        }

//        return null;
//    }
//}

using FoodStreet.Models;

namespace FoodStreet.Services;

public class GeofenceService
{
    public List<Poi> CheckNearby(Location userLocation, List<Poi> pois)
    {
        List<Poi> nearby = new();

        foreach (var poi in pois)
        {
            var distanceKm = Location.CalculateDistance(
                userLocation,
                new Location(poi.Latitude, poi.Longitude),
                DistanceUnits.Kilometers);

            var distanceMeters = distanceKm * 1000;

            Console.WriteLine($"User: {userLocation.Latitude}, {userLocation.Longitude}");
            Console.WriteLine($"POI: {poi.Latitude}, {poi.Longitude}");
            Console.WriteLine($"Khoảng cách: {distanceMeters} m");

            if (distanceMeters <= poi.Radius)
            {
                nearby.Add(poi);
            }
        }

        return nearby;
    }
}
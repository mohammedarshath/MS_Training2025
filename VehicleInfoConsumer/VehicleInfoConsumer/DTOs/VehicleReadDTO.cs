﻿

namespace VehicleInfoConsumer.DTOs
{
    public enum FuelType
    {
        PETROL, DIESEL, EV, GAS
    }
    public record VehicleReadDTO(string RegNo,string model, DateTime dor,  string chassisNo,string engineNo, string color, FuelType fuelType);
}

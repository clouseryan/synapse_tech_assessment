namespace SynapseTechAssessment.Data.Models;

public record Order
{
    public string Device { get; set; }
    public string Liters { get; set; }
    public string Usage { get; set; }
    public string Diagnosis { get; set; }
    public string OrderingProvider { get; set; }
    public string PatientName { get; set; }
    public string Dob { get; set; }
}



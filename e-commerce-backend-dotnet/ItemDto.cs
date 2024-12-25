namespace e_commerce_backend_dotnet;

public class Item(int Id, string Name)
{
    public int Id { get; set; } = Id;
    public string Name { get; set; } = Name;

}

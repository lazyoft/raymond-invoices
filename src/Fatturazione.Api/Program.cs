using Fatturazione.Api.Endpoints;
using Fatturazione.Domain.Services;
using Fatturazione.Infrastructure.Data;
using Fatturazione.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Fatturazione API",
        Version = "v1",
        Description = "Italian Invoicing Application"
    });
});

// Register data store as singleton
builder.Services.AddSingleton<InMemoryDataStore>();

// Register repositories
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IIssuerProfileRepository, IssuerProfileRepository>();

// Register domain services
builder.Services.AddScoped<IRitenutaService, RitenutaService>();
builder.Services.AddScoped<IBolloService, BolloService>();
builder.Services.AddScoped<IInvoiceCalculationService, InvoiceCalculationService>();
builder.Services.AddScoped<IInvoiceNumberingService, InvoiceNumberingService>();
builder.Services.AddScoped<ICreditNoteService, CreditNoteService>();
builder.Services.AddScoped<IDocumentDiscountService, DocumentDiscountService>();
builder.Services.AddScoped<IFatturaPAXmlService, FatturaPAXmlService>();

// Configure CORS for demo purposes
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Seed demo data
SeedData(app.Services);

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Fatturazione API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseCors();
app.UseHttpsRedirection();

// Map endpoint groups
var apiGroup = app.MapGroup("/api");

apiGroup.MapGroup("/clients")
    .MapClientEndpoints()
    .WithTags("Clients");

apiGroup.MapGroup("/invoices")
    .MapInvoiceEndpoints()
    .WithTags("Invoices");

apiGroup.MapGroup("/issuer-profile")
    .MapIssuerProfileEndpoints()
    .WithTags("IssuerProfile");

// Root endpoint
app.MapGet("/", () => Results.Redirect("/swagger"))
    .WithName("Root")
    .ExcludeFromDescription();

app.Run();

// Seed demo data method
static void SeedData(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var dataStore = scope.ServiceProvider.GetRequiredService<InMemoryDataStore>();
    var clientRepository = scope.ServiceProvider.GetRequiredService<IClientRepository>();
    var invoiceRepository = scope.ServiceProvider.GetRequiredService<IInvoiceRepository>();
    var calculationService = scope.ServiceProvider.GetRequiredService<IInvoiceCalculationService>();

    // Create sample clients
    var client1 = new Fatturazione.Domain.Models.Client
    {
        Id = Guid.NewGuid(),
        RagioneSociale = "Studio Rossi & Associati",
        PartitaIva = "12345678903", // Valid Italian VAT number
        CodiceFiscale = "RSSMRA70A01H501S",
        ClientType = Fatturazione.Domain.Models.ClientType.Professional,
        Email = "info@studiorossi.it",
        Phone = "+39 02 1234567",
        SubjectToRitenuta = true,
        RitenutaPercentage = 20.0m,
        Address = new Fatturazione.Domain.Models.Address
        {
            Street = "Via Roma 123",
            City = "Milano",
            Province = "MI",
            PostalCode = "20121",
            Country = "Italia"
        }
    };

    var client2 = new Fatturazione.Domain.Models.Client
    {
        Id = Guid.NewGuid(),
        RagioneSociale = "TechSolutions SRL",
        PartitaIva = "98765432109", // Valid Italian VAT number
        ClientType = Fatturazione.Domain.Models.ClientType.Company,
        Email = "contatti@techsolutions.it",
        Phone = "+39 06 9876543",
        SubjectToRitenuta = false,
        Address = new Fatturazione.Domain.Models.Address
        {
            Street = "Corso Italia 456",
            City = "Roma",
            Province = "RM",
            PostalCode = "00184",
            Country = "Italia"
        }
    };

    var client3 = new Fatturazione.Domain.Models.Client
    {
        Id = Guid.NewGuid(),
        RagioneSociale = "Freelance Designer - Anna Bianchi",
        PartitaIva = "11223344556", // Valid Italian VAT number
        CodiceFiscale = "BNCNNA85M45F205X",
        ClientType = Fatturazione.Domain.Models.ClientType.Professional,
        Email = "anna.bianchi@design.it",
        Phone = "+39 011 5555555",
        SubjectToRitenuta = true,
        RitenutaPercentage = 20.0m,
        Address = new Fatturazione.Domain.Models.Address
        {
            Street = "Piazza Castello 78",
            City = "Torino",
            Province = "TO",
            PostalCode = "10121",
            Country = "Italia"
        }
    };

    var client4 = new Fatturazione.Domain.Models.Client
    {
        Id = Guid.NewGuid(),
        RagioneSociale = "Comune di Firenze",
        PartitaIva = "01234567890", // Valid Italian VAT number
        ClientType = Fatturazione.Domain.Models.ClientType.PublicAdministration,
        Email = "protocollo@comune.firenze.it",
        Phone = "+39 055 1234567",
        SubjectToRitenuta = false,
        SubjectToSplitPayment = true,
        CodiceUnivocoUfficio = "UFXZ0L",
        Address = new Fatturazione.Domain.Models.Address
        {
            Street = "Piazza della Signoria 1",
            City = "Firenze",
            Province = "FI",
            PostalCode = "50122",
            Country = "Italia"
        }
    };

    clientRepository.CreateAsync(client1).Wait();
    clientRepository.CreateAsync(client2).Wait();
    clientRepository.CreateAsync(client3).Wait();
    clientRepository.CreateAsync(client4).Wait();

    // Create sample invoices

    // Invoice 1: Professional with ritenuta
    var invoice1 = new Fatturazione.Domain.Models.Invoice
    {
        Id = Guid.NewGuid(),
        InvoiceNumber = "2026/001",
        InvoiceDate = new DateTime(2026, 1, 15),
        DueDate = new DateTime(2026, 2, 14),
        ClientId = client1.Id,
        Client = client1,
        Status = Fatturazione.Domain.Models.InvoiceStatus.Issued,
        Notes = "Consulenza legale trimestre Q1 2026",
        Items = new List<Fatturazione.Domain.Models.InvoiceItem>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Description = "Consulenza legale - Gennaio 2026",
                Quantity = 20,
                UnitPrice = 150.00m,
                IvaRate = Fatturazione.Domain.Models.IvaRate.Standard
            },
            new()
            {
                Id = Guid.NewGuid(),
                Description = "Pratica contrattuale",
                Quantity = 5,
                UnitPrice = 200.00m,
                IvaRate = Fatturazione.Domain.Models.IvaRate.Standard
            }
        }
    };

    calculationService.CalculateInvoiceTotals(invoice1);
    invoiceRepository.CreateAsync(invoice1).Wait();

    // Invoice 2: Company without ritenuta
    var invoice2 = new Fatturazione.Domain.Models.Invoice
    {
        Id = Guid.NewGuid(),
        InvoiceNumber = "2026/002",
        InvoiceDate = new DateTime(2026, 1, 20),
        DueDate = new DateTime(2026, 3, 20),
        ClientId = client2.Id,
        Client = client2,
        Status = Fatturazione.Domain.Models.InvoiceStatus.Sent,
        Notes = "Sviluppo software personalizzato",
        Items = new List<Fatturazione.Domain.Models.InvoiceItem>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Description = "Sviluppo modulo CRM",
                Quantity = 80,
                UnitPrice = 85.00m,
                IvaRate = Fatturazione.Domain.Models.IvaRate.Standard
            },
            new()
            {
                Id = Guid.NewGuid(),
                Description = "Testing e QA",
                Quantity = 20,
                UnitPrice = 65.00m,
                IvaRate = Fatturazione.Domain.Models.IvaRate.Standard
            }
        }
    };

    calculationService.CalculateInvoiceTotals(invoice2);
    invoiceRepository.CreateAsync(invoice2).Wait();

    // Invoice 3: Draft invoice with designer
    var invoice3 = new Fatturazione.Domain.Models.Invoice
    {
        Id = Guid.NewGuid(),
        InvoiceDate = new DateTime(2026, 2, 1),
        DueDate = new DateTime(2026, 3, 3),
        ClientId = client3.Id,
        Client = client3,
        Status = Fatturazione.Domain.Models.InvoiceStatus.Draft,
        Notes = "Design branding completo",
        Items = new List<Fatturazione.Domain.Models.InvoiceItem>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Description = "Logo design",
                Quantity = 1,
                UnitPrice = 800.00m,
                IvaRate = Fatturazione.Domain.Models.IvaRate.Standard
            },
            new()
            {
                Id = Guid.NewGuid(),
                Description = "Brand guidelines",
                Quantity = 1,
                UnitPrice = 600.00m,
                IvaRate = Fatturazione.Domain.Models.IvaRate.Standard
            },
            new()
            {
                Id = Guid.NewGuid(),
                Description = "Business cards design",
                Quantity = 1,
                UnitPrice = 150.00m,
                IvaRate = Fatturazione.Domain.Models.IvaRate.Standard
            }
        }
    };

    calculationService.CalculateInvoiceTotals(invoice3);
    invoiceRepository.CreateAsync(invoice3).Wait();

    Console.WriteLine("✓ Seed data created successfully");
    Console.WriteLine($"  - {dataStore.Clients.Count} clients");
    Console.WriteLine($"  - {dataStore.Invoices.Count} invoices");
}

// Make Program class accessible for integration tests
public partial class Program { }

using System.Net;
using HtmlAgilityPack;
using PassApp;


var people = new List<Person>
{
    new Person{Forman = "Kajetan", Efternamn="Kazimierczak"},
  
};

var myOrder = new Order()
{
    ContactPhone = "", 
    ContactEmail = "", 
    People = people,
    PassportOffices = new List<PassportOffice>
    {
        PassportOffice.SthlmCity,
       // PassportOffice.Solna,
       // PassportOffice.Rinkeby
    },
    NotLaterThan = new DateOnly(2022,5,10)
};

Console.WriteLine("Hello, World!");


var cookieContainer = new CookieContainer();
var httpClientHandler = new HttpClientHandler{CookieContainer = cookieContainer};
var httpClient = new HttpClient(httpClientHandler);
httpClient.DefaultRequestHeaders.Add("Referer", "https://bokapass.nemoq.se/Booking/Booking/Index/Stockholm");
httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/99.0.4844.74 Safari/537.36");
httpClient.DefaultRequestHeaders.Add("Origin", "https://bokapass.nemoq.se");

await Initialize(httpClient, myOrder);

while (true)
{
    var (date, timeBooking) = await NextFreeTime(httpClient, myOrder);
    if (date < myOrder.NotLaterThan && timeBooking != null)
    {
        var success = await BookAsync(httpClient, timeBooking, myOrder);
        if (success)
        {
            Console.WriteLine("Bokat");
            return; // exit
        }
    }
    await Task.Delay(new Random().Next(7000, 15000));
}


async Task<bool> BookAsync(HttpClient client, TimeBooking timeBooking, Order order)
{
    Console.WriteLine($"Forsöker boka {timeBooking.ReservedDateTime} för {timeBooking.NumberOfPeople} personer i {((PassportOffice)(int.Parse(timeBooking.ReservedSectionId))).ToString()}");
   
    var keyValues = new[]
    {
        new KeyValuePair<string, string>("FormId", timeBooking.FormId),
        new KeyValuePair<string, string>("ReservedServiceTypeId", timeBooking.ReservedServiceTypeId),
        new KeyValuePair<string, string>("ReservedSectionId", timeBooking.ReservedSectionId),
        new KeyValuePair<string, string>("NQServiceTypeId", timeBooking.NQServiceTypeId),
        new KeyValuePair<string, string>("SectionId", timeBooking.SectionId),
        new KeyValuePair<string, string>("FromDateString", DateTime.Parse(timeBooking.ReservedDateTime).ToString("yyyy-MM-dd")),
        new KeyValuePair<string, string>("NumberOfPeople", timeBooking.NumberOfPeople),
        new KeyValuePair<string, string>("SearchTimeHour", timeBooking.SearchTimeHour),
        new KeyValuePair<string, string>("RegionId", timeBooking.RegionId),
        new KeyValuePair<string, string>("Next", timeBooking.Next),
        new KeyValuePair<string, string>("ReservedDateTime", timeBooking.ReservedDateTime),
      
    };
    var formContent = new FormUrlEncodedContent(keyValues);
    var result = await client.PostAsync("https://bokapass.nemoq.se/Booking/Booking/Next/Stockholm", formContent);
    var response = await result.Content.ReadAsStringAsync();
    if (response.Contains("Tiden du valde &#228;r inte tillg&#228;nglig. Var god v&#228;lj en ny tid"))
    {
        Console.WriteLine("Tiden finns inte kvar :(");
        return false;
    }

    if (!response.Contains("Uppgifter till bokningen"))
    {
        Console.WriteLine("Något gick fel :(");
        return false;
    }

    await Task.Delay(new Random().Next(1000, 3000));

    Console.WriteLine("Skickar uppgifter till bokningen");
    var formContent2 = getContentForUppgifterTillBokningen(order);
    var result2 = await client.PostAsync("https://bokapass.nemoq.se/Booking/Booking/Next/Stockholm", formContent2);
    var response2 = await result2.Content.ReadAsStringAsync();
    if (!response2.Contains("Viktig information"))
    {
        Console.WriteLine("Något gick fel :(");
        return false;
    }

    await Task.Delay(new Random().Next(1000, 3000));
    Console.WriteLine("Bekräftar");
    var formContent3 = new FormUrlEncodedContent(new[]
    {
        new KeyValuePair<string, string>("Next", "Nästa"),
        
    });
    var result3 = await client.PostAsync("https://bokapass.nemoq.se/Booking/Booking/Next/Stockholm", formContent3);
    var response3 = await result3.Content.ReadAsStringAsync();
    if (!response3.Contains("Kontaktuppgifter"))
    {
        Console.WriteLine("Något gick fel :(");
        return false;
    }

    await Task.Delay(new Random().Next(1000, 3000));
    Console.WriteLine("Fyller i kontaktuppgifter");

    var formContent4 = getContentForKontaktuppgifter(order);
    var result4 = await client.PostAsync("https://bokapass.nemoq.se/Booking/Booking/Next/Stockholm", formContent4);
    var response4 = await result4.Content.ReadAsStringAsync();
    if (!response4.Contains("Bekräfta bokning"))
    {
        Console.WriteLine("Något gick fel :(");
        return false;
    }

    await Task.Delay(new Random().Next(1000, 3000));
    Console.WriteLine("Bekräftar bokning");
    var formContent5 = getContentForConfirm(order);
    var result5 = await client.PostAsync("https://bokapass.nemoq.se/Booking/Booking/Next/Stockholm", formContent5);
    var response5 = await result5.Content.ReadAsStringAsync();
    if (response5.Contains("N&#229;gonting gick fel, bokningen gick inte att genomf&#246;ra"))
    {
        Console.WriteLine("Något gick fel :(");
        return false;
    }

    return true;
}

async Task<(DateOnly Date, TimeBooking? TimeBooking)> NextFreeTime(HttpClient client, Order order)
{
    
    Console.Write("Letar efter nästa tid...");
    var sectionId = (order.PassportOffices.Count == 1 ? (int)order.PassportOffices.First() : 0).ToString();

    var formContent = new FormUrlEncodedContent(new[]
    {
        new KeyValuePair<string, string>("FormId", "1"),
        new KeyValuePair<string, string>("NumberOfPeople", order.People.Count.ToString()),
        new KeyValuePair<string, string>("RegionId", "0"),
        new KeyValuePair<string, string>("SectionId", sectionId),
        new KeyValuePair<string, string>("NQServiceTypeId", "1"),
        new KeyValuePair<string, string>("FromDateString", DateTime.Now.ToString("yyyy-MM-dd")),
        new KeyValuePair<string, string>("SearchTimeHour", "10"),
        new KeyValuePair<string, string>("TimeSearchFirstAvailableButton", "Första lediga tid"),
    });
    var result = await client.PostAsync("https://bokapass.nemoq.se/Booking/Booking/Next/Stockholm", formContent);
    var response = await result.Content.ReadAsStringAsync();

    if (response.Contains("Du har gjort f&#246;r m&#229;nga &#39;f&#246;rsta lediga tid&#39; s&#246;kningar, var v&#228;nlig och v&#228;nta en stund."))
    {
        Console.WriteLine("Du har gjort för många 'första lediga tid' sökningar, var vänlig och vänta en stund.");
        Console.WriteLine("Väntar några sekunder");
        await Task.Delay(new Random().Next(8000, 12000));
        return (DateOnly.MaxValue, null);
    }
    
    var doc = new HtmlDocument();
    doc.LoadHtml(response);

    string dateString = doc.DocumentNode
        .SelectSingleNode("//div/input[@name=\"FromDateString\"]")
        .Attributes["value"].Value;

    Console.WriteLine($"hittade {dateString}");
    
    var timeBooking = GetEarliestTimeBooking(response, order);

    var date = DateOnly.Parse(timeBooking?.ReservedDateTime[..10] ?? dateString);

    return (date, timeBooking);

}

TimeBooking? GetEarliestTimeBooking(string webContent, Order order)
{
    var doc = new HtmlDocument();
    doc.LoadHtml(webContent);

    var nodes = doc.DocumentNode
        .SelectNodes("//div[@data-function=\"timeTableCell\"]");
    if (nodes == null) return null;


    var timeBookings = new List<TimeBooking>();
    foreach (var node in nodes)
    {
        if (node.Attributes["aria-label"].Value != "Bokad")
        {
            timeBookings.Add(new TimeBooking
            {
                ReservedServiceTypeId = node.Attributes["data-servicetypeid"].Value,
                ReservedSectionId = node.Attributes["data-sectionid"].Value,
                ReservedDateTime = node.Attributes["data-fromdatetime"].Value,
                NumberOfPeople = order.People.Count.ToString()
            });
        }

    }

    var timeBooking = timeBookings.OrderBy(x => x.ReservedDateTime)
        .FirstOrDefault(x =>
        order.PassportOffices.Contains((PassportOffice)(int.Parse(x.ReservedSectionId))));

    return timeBooking;
}


async Task Initialize(HttpClient client, Order order)
{
    // ladda förstasidan, få cookies
    Console.WriteLine("Laddar startsidan");
    var result1 = await client.GetAsync("https://bokapass.nemoq.se/Booking/Booking/Index/Stockholm");
    var response = await result1.Content.ReadAsStringAsync();
    await Task.Delay(500);


    // välj boka ny tid
    Console.WriteLine("Klickar på ny tid");
    var formContent = new FormUrlEncodedContent(new[]
    {
        new KeyValuePair<string, string>("FormId", "1"),
        new KeyValuePair<string, string>("ServiceGroupId", "47"),
        new KeyValuePair<string, string>("StartNextButton", "Boka ny tid"),
    });

    var result2 = await client.PostAsync("https://bokapass.nemoq.se/Booking/Booking/Next/Stockholm", formContent);
    var response2 = await result2.Content.ReadAsStringAsync();
    await Task.Delay(700);


    // Acceptera och informationsbehandling och välj antal personer
    Console.WriteLine($"Väljer {order.People.Count} personer och accepterar GDPR");
    var formContent2 = new FormUrlEncodedContent(new[]
    {
        new KeyValuePair<string, string>("AgreementText", "För att kunna genomföra tidsbokning för ansökan om pass och/eller id-kort krävs att dina personuppgifter behandlas. Det är nödvändigt för att Polismyndigheten ska kunna utföra de uppgifter som följer av passförordningen (1979:664) och förordningen (2006:661) om nationellt identitetskort och som ett led i myndighetsutövning. För att åtgärda eventuellt uppkomna fel kan också systemleverantören komma att nås av personuppgifterna. Samtliga uppgifter raderas ur tidsbokningssystemet dagen efter besöket."),
        new KeyValuePair<string, string>("AcceptInformationStorage", "true"),
        new KeyValuePair<string, string>("AcceptInformationStorage", "false"),
        new KeyValuePair<string, string>("NumberOfPeople", order.People.Count.ToString()),
        new KeyValuePair<string, string>("Next", "Nästa"),
    });
    var result3 = await client.PostAsync("https://bokapass.nemoq.se/Booking/Booking/Next/Stockholm", formContent2);
    var response3 = await result3.Content.ReadAsStringAsync();
    await Task.Delay(1000);
    
    // Fyll i att alla bor i Sverige
    Console.WriteLine($"Fyller i att alla {order.People.Count} bor i Sverige");
    var formContent3 = getContentForEveryoneLivesInSweden(order);
    var result4 = await client.PostAsync("https://bokapass.nemoq.se/Booking/Booking/Next/Stockholm", formContent3);
    var response4 = await result4.Content.ReadAsStringAsync();
    await Task.Delay(700);
    if (!response4.Contains("Välj tid"))
    {
        throw new Exception("Det sket sig");
    }
}

FormUrlEncodedContent getContentForKontaktuppgifter(Order order)
{
    List<KeyValuePair<string, string>> keyValues = new List<KeyValuePair<string, string>>();

    keyValues.Add(new KeyValuePair<string, string>("EmailAddress", order.ContactEmail));
    keyValues.Add(new KeyValuePair<string, string>("ConfirmEmailAddress", order.ContactEmail));
    keyValues.Add(new KeyValuePair<string, string>("PhoneNumber", order.ContactPhone));
    keyValues.Add(new KeyValuePair<string, string>("ConfirmPhoneNumber", order.ContactPhone));

    keyValues.Add(new KeyValuePair<string, string>("SelectedContacts[0].IsSelected", "true"));
    keyValues.Add(new KeyValuePair<string, string>("SelectedContacts[0].IsSelected", "false"));
    keyValues.Add(new KeyValuePair<string, string>("SelectedContacts[0].MessageTypeId", "2"));
    keyValues.Add(new KeyValuePair<string, string>("SelectedContacts[0].MessageKindId", "1"));
    keyValues.Add(new KeyValuePair<string, string>("SelectedContacts[0].TextName", "MESSAGETYPE_EMAIL"));
    
    keyValues.Add(new KeyValuePair<string, string>("SelectedContacts[1].IsSelected", "false"));
    keyValues.Add(new KeyValuePair<string, string>("SelectedContacts[1].MessageTypeId", "1"));
    keyValues.Add(new KeyValuePair<string, string>("SelectedContacts[1].MessageKindId", "1"));
    keyValues.Add(new KeyValuePair<string, string>("SelectedContacts[1].TextName", "MESSAGETYPE_SMS"));
    
    keyValues.Add(new KeyValuePair<string, string>("SelectedContacts[2].IsSelected", "true"));
    keyValues.Add(new KeyValuePair<string, string>("SelectedContacts[2].IsSelected", "false"));
    keyValues.Add(new KeyValuePair<string, string>("SelectedContacts[2].MessageTypeId", "2"));
    keyValues.Add(new KeyValuePair<string, string>("SelectedContacts[2].MessageKindId", "2"));
    keyValues.Add(new KeyValuePair<string, string>("SelectedContacts[2].TextName", "MESSAGETYPE_EMAIL"));
    
    keyValues.Add(new KeyValuePair<string, string>("SelectedContacts[3].IsSelected", "false"));
    keyValues.Add(new KeyValuePair<string, string>("SelectedContacts[3].MessageTypeId", "1"));
    keyValues.Add(new KeyValuePair<string, string>("SelectedContacts[3].MessageKindId", "2"));
    keyValues.Add(new KeyValuePair<string, string>("SelectedContacts[3].TextName", "MESSAGETYPE_SMS"));
    keyValues.Add(new KeyValuePair<string, string>("ReminderOption", "24"));
    keyValues.Add(new KeyValuePair<string, string>("Next", "Nästa"));
    return new FormUrlEncodedContent(keyValues);

}

FormUrlEncodedContent getContentForEveryoneLivesInSweden(Order order)
{
    List<KeyValuePair<string, string>> keyValues = new List<KeyValuePair<string, string>>();
    var i = 0;
    foreach (var person in order.People)
    {
        keyValues.Add(new KeyValuePair<string, string>($"ServiceCategoryCustomers[{i}].CustomerIndex", $"{i}"));
        keyValues.Add(new KeyValuePair<string, string>($"ServiceCategoryCustomers[{i}].ServiceCategoryId", "2"));
        i++;
    }
    keyValues.Add(new KeyValuePair<string, string>("Next", "Nästa"));
    return new FormUrlEncodedContent(keyValues);

}

FormUrlEncodedContent getContentForUppgifterTillBokningen(Order order)
{
    List<KeyValuePair<string, string>> keyValues = new List<KeyValuePair<string, string>>();
    var i = 0;
    foreach (var person in order.People)
    {
        keyValues.Add(new KeyValuePair<string, string>($"Customers[{i}].BookingCustomerId", "0"));
        keyValues.Add(new KeyValuePair<string, string>($"Customers[{i}].BookingFieldValues[0].Value", $"{person.Forman}"));
        keyValues.Add(new KeyValuePair<string, string>($"Customers[{i}].BookingFieldValues[0].BookingFieldId", "5"));
        keyValues.Add(new KeyValuePair<string, string>($"Customers[{i}].BookingFieldValues[0].BookingFieldTextName", "BF_2_FÖRNAMN"));
        keyValues.Add(new KeyValuePair<string, string>($"Customers[{i}].BookingFieldValues[0].FieldTypeId", "1"));
        keyValues.Add(new KeyValuePair<string, string>($"Customers[{i}].BookingFieldValues[1].Value", $"{person.Efternamn}"));
        keyValues.Add(new KeyValuePair<string, string>($"Customers[{i}].BookingFieldValues[1].BookingFieldId", "6"));
        keyValues.Add(new KeyValuePair<string, string>($"Customers[{i}].BookingFieldValues[1].BookingFieldTextName", "BF_2_EFTERNAMN"));
        keyValues.Add(new KeyValuePair<string, string>($"Customers[{i}].BookingFieldValues[1].FieldTypeId", "1"));
        keyValues.Add(new KeyValuePair<string, string>($"Customers[{i}].Services[0].IsSelected", "true"));
        keyValues.Add(new KeyValuePair<string, string>($"Customers[{i}].Services[0].IsSelected", "false"));
        keyValues.Add(new KeyValuePair<string, string>($"Customers[{i}].Services[0].ServiceId", "52"));
        keyValues.Add(new KeyValuePair<string, string>($"Customers[{i}].Services[0].ServiceTextName", "SERVICE_2_PASSANSÖKANSTOCKHOLMS"));
        keyValues.Add(new KeyValuePair<string, string>($"Customers[{i}].Services[1].IsSelected", "false"));
        keyValues.Add(new KeyValuePair<string, string>($"Customers[{i}].Services[1].ServiceId", "48"));
        keyValues.Add(new KeyValuePair<string, string>($"Customers[{i}].Services[1].ServiceTextName", "SERVICE_2_ID-KORTSTOCKHOLMS"));
        i++;
    }
    keyValues.Add(new KeyValuePair<string, string>("Next", "Nästa"));
    return new FormUrlEncodedContent(keyValues);

}

FormUrlEncodedContent getContentForConfirm(Order order)
{
    List<KeyValuePair<string, string>> keyValues = new List<KeyValuePair<string, string>>();
    
    keyValues.Add(new KeyValuePair<string, string>("Next", "Bekräfta bokning"));

    var i = 0;
    foreach (var person in order.People)
    {
        keyValues.Add(new KeyValuePair<string, string>($"PersonViewModel.Customers[{i}].Services[0].IsSelected", "false"));
        keyValues.Add(new KeyValuePair<string, string>($"PersonViewModel.Customers[{i}].Services[1].IsSelected", "false"));
    }

    for (int j = 0; j <= 3; j++)
    {
        keyValues.Add(new KeyValuePair<string, string>($"ContactViewModel.SelectedContacts[{j}].IsSelected", "false"));
    }
    
    return new FormUrlEncodedContent(keyValues);

}

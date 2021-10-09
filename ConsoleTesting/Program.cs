// See https://aka.ms/new-console-template for more information
using System.Xml.Serialization;
using Subrom.Domain.Datfiles.Xml;

Console.WriteLine("Hello, World!");

//const string filename = @"C:\working\dats\MAME 0.235 Software List CHDs (merged) (dir2dat).dat";

const string filename = @"C:\working\dats\MAME 0.235 ROMs (merged).xml";
//const string filename = @"C:\working\dats\Acorn Archimedes - Games - [ADF] (TOSEC-v2021-02-12_CM).dat";

var serializer = new XmlSerializer(typeof(DatafileElement));
using (var fileStream = new FileStream(filename, FileMode.Open)) {
	var result = (DatafileElement)serializer.Deserialize(fileStream);
	Console.WriteLine("Hello, World!444444");
}

Console.WriteLine("Hello, World!446666");

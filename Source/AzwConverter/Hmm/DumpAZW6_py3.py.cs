using MobiMetadataReader.Net.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public static class DumpAZW6_py3 {
    
    public static string get_image_type(string imgname, byte[] imgdata = null) {
        var imgtype = ".jpg";
        // horrible hack since imghdr detects jxr/wdp as tiffs
        if (imgtype != null && imgtype == "tiff") {
            imgtype = "wdp";
        }
        return imgtype;
    }
    
    public static void processCRES(int i, string data) {
        data = data[12];
        var imgtype = get_image_type(null, data);
        if (imgtype == "jpeg") {
            imgtype = "jpg";
        }
        if (imgtype == null) {
            Console.WriteLine(String.Format("        Warning: CRES Section %s does not contain a recognised resource", i));
            imgtype = "dat";
        }
        var imgname = String.Format("HDimage%05d.%s", i, imgtype);
        var imgdir = os.path.join(".", "azw6_images");
        if (!os.path.exists(imgdir)) {
            os.mkdir(imgdir);
        }
        Console.WriteLine("        Extracting HD image: {0:s} from section {1:d}".format(imgname, i));
        var imgpath = os.path.join(imgdir, imgname);
        using (var f = open(imgpath, "wb")) {
            f.write(data);
        }
    }
    
    // this is just guesswork so far, making big assumption that
    // metavalue key numbers reamin the same in the CONT EXTH
    public static void dump_contexth(string codec, object extheader) {
        object name;
        // determine text encoding
        if (extheader == "") {
            return null;
        }
        var id_map_strings = new Dictionary<object, object> {
            {
                1,
                "Drm Server Id (1)"},
            {
                2,
                "Drm Commerce Id (2)"},
            {
                3,
                "Drm Ebookbase Book Id(3)"},
            {
                100,
                "Creator_(100)"},
            {
                101,
                "Publisher_(101)"},
            {
                102,
                "Imprint_(102)"},
            {
                103,
                "Description_(103)"},
            {
                104,
                "ISBN_(104)"},
            {
                105,
                "Subject_(105)"},
            {
                106,
                "Published_(106)"},
            {
                107,
                "Review_(107)"},
            {
                108,
                "Contributor_(108)"},
            {
                109,
                "Rights_(109)"},
            {
                110,
                "SubjectCode_(110)"},
            {
                111,
                "Type_(111)"},
            {
                112,
                "Source_(112)"},
            {
                113,
                "ASIN_(113)"},
            {
                114,
                "versionNumber_(114)"},
            {
                117,
                "Adult_(117)"},
            {
                118,
                "Price_(118)"},
            {
                119,
                "Currency_(119)"},
            {
                122,
                "fixed-layout_(122)"},
            {
                123,
                "book-type_(123)"},
            {
                124,
                "orientation-lock_(124)"},
            {
                126,
                "original-resolution_(126)"},
            {
                127,
                "zero-gutter_(127)"},
            {
                128,
                "zero-margin_(128)"},
            {
                129,
                "K8_Masthead/Cover_Image_(129)"},
            {
                132,
                "RegionMagnification_(132)"},
            {
                200,
                "DictShortName_(200)"},
            {
                208,
                "Watermark_(208)"},
            {
                501,
                "cdeType_(501)"},
            {
                502,
                "last_update_time_(502)"},
            {
                503,
                "Updated_Title_(503)"},
            {
                504,
                "ASIN_(504)"},
            {
                508,
                "Unknown_Title_Furigana?_(508)"},
            {
                517,
                "Unknown_Creator_Furigana?_(517)"},
            {
                522,
                "Unknown_Publisher_Furigana?_(522)"},
            {
                524,
                "Language_(524)"},
            {
                525,
                "primary-writing-mode_(525)"},
            {
                526,
                "Unknown_(526)"},
            {
                527,
                "page-progression-direction_(527)"},
            {
                528,
                "override-kindle_fonts_(528)"},
            {
                529,
                "Unknown_(529)"},
            {
                534,
                "Input_Source_Type_(534)"},
            {
                535,
                "Kindlegen_BuildRev_Number_(535)"},
            {
                536,
                "Container_Info_(536)"},
            {
                538,
                "Container_Resolution_(538)"},
            {
                539,
                "Container_Mimetype_(539)"},
            {
                542,
                "Unknown_but_changes_with_filename_only_(542)"},
            {
                543,
                "Container_id_(543)"},
            {
                544,
                "Unknown_(544)"}};
        var id_map_values = new Dictionary<object, object> {
            {
                115,
                "sample_(115)"},
            {
                116,
                "StartOffset_(116)"},
            {
                121,
                "K8(121)_Boundary_Section_(121)"},
            {
                125,
                "K8_Count_of_Resources_Fonts_Images_(125)"},
            {
                131,
                "K8_Unidentified_Count_(131)"},
            {
                201,
                "CoverOffset_(201)"},
            {
                202,
                "ThumbOffset_(202)"},
            {
                203,
                "Fake_Cover_(203)"},
            {
                204,
                "Creator_Software_(204)"},
            {
                205,
                "Creator_Major_Version_(205)"},
            {
                206,
                "Creator_Minor_Version_(206)"},
            {
                207,
                "Creator_Build_Number_(207)"},
            {
                401,
                "Clipping_Limit_(401)"},
            {
                402,
                "Publisher_Limit_(402)"},
            {
                404,
                "Text_to_Speech_Disabled_(404)"}};
        var id_map_hexstrings = new Dictionary<object, object> {
            {
                209,
                "Tamper_Proof_Keys_(209_in_hex)"},
            {
                300,
                "Font_Signature_(300_in_hex)"}};
        (_length, num_items) = @struct.unpack(">LL", extheader[4::12]);
        extheader = extheader[12];
        var pos = 0;
        foreach (var _ in Enumerable.Range(0, num_items)) {
            (id_, size) = @struct.unpack(">LL", extheader[pos::(pos  +  8)]);
            var content = extheader[(pos  +  8)::(pos  +  size)];
            if (id_map_strings.Contains(id_)) {
                name = id_map_strings[id_];
                Console.WriteLine(String.Format("\n    Key: \"%s\"\n        Value: \"%s\"", name, Encoding.GetEncoding(codec).GetString(content).encode("utf-8")));
            } else if (id_map_values.Contains(id_)) {
                name = id_map_values[id_];
                if (size == 9) {
                    ValueTuple.Create(value) = @struct.unpack("B", content);
                    Console.WriteLine(String.Format("\n    Key: \"%s\"\n        Value: 0x%01x", name, value));
                } else if (size == 10) {
                    ValueTuple.Create(value) = @struct.unpack(">H", content);
                    Console.WriteLine(String.Format("\n    Key: \"%s\"\n        Value: 0x%02x", name, value));
                } else if (size == 12) {
                    ValueTuple.Create(value) = @struct.unpack(">L", content);
                    Console.WriteLine(String.Format("\n    Key: \"%s\"\n        Value: 0x%04x", name, value));
                } else {
                    Console.WriteLine(String.Format("\nError: Value for %s has unexpected size of %s", name, size));
                }
            } else if (id_map_hexstrings.Contains(id_)) {
                name = id_map_hexstrings[id_];
                Console.WriteLine(String.Format("\n    Key: \"%s\"\n        Value: 0x%s", name, content.encode("hex")));
            } else {
                Console.WriteLine(String.Format("\nWarning: Unknown metadata with id %s found", id_));
                name = id_.ToString() + " (hex)";
                Console.WriteLine(String.Format("    Key: \"%s\"\n        Value: 0x%s", name, content.encode("hex")));
            }
            pos += size;
        }
    }
    
    public static object sortedHeaderKeys(object mheader) {
        var hdrkeys = mheader.keys().ToList().OrderBy(akey => mheader[akey][0]).ToList();
        return hdrkeys;
    }
    
    public class dumpHeaderException
        : Exception {
    }
    
    public class PalmDB {
        
        public object data;
        
        public object unique_id_seed = 68;
        
        public object number_of_pdb_records = 76;
        
        public object first_pdb_record = 78;
        
        public PalmDB(string palmdata) {
            this.data = palmdata;
            ValueTuple.Create(this.nsec) = @struct.unpack_from(">H", this.data, PalmDB.number_of_pdb_records);
        }
        
        public virtual Tuple<object, object> getsecaddr(int secno) {
            ValueTuple.Create(secstart) = @struct.unpack_from(">L", this.data, PalmDB.first_pdb_record + secno * 8);
            if (secno == this.nsec - 1) {
                var secend = this.data.Count;
            } else {
                ValueTuple.Create(secend) = @struct.unpack_from(">L", this.data, PalmDB.first_pdb_record + (secno + 1) * 8);
            }
            return (secstart, secend);
        }
        
        public virtual string readsection(int secno) {
            if (secno < this.nsec) {
                (secstart, secend) = this.getsecaddr(secno);
                return this.data[secstart::secend];
            }
            return "";
        }
        
        public virtual object getnumsections() {
            return this.nsec;
        }
    }
    
    public class HdrParser {
        
        public string codec;
        
        public Dictionary<int, string> codec_map;
        
        //public object cont_header;
        
        public object exth;
        
        public Dictionary<object, object> hdr;
        
        public object header;
        
        public object header_sorted_keys;
        
        public object start;
        
        public object title;
        
        public object title_length;
        
        public object title_offset;
        
        public static object cont_header = new Dictionary<object, object> {
            {
                "magic",
                (0x00, "4s", 4)},
            {
                "record_size",
                (0x04, ">L", 4)},
            {
                "type",
                (0x08, ">H", 2)},
            {
                "count",
                (0x0A, ">H", 2)},
            {
                "codepage",
                (0x0C, ">L", 4)},
            {
                "unknown0",
                (0x10, ">L", 4)},
            {
                "unknown1",
                (0x14, ">L", 4)},
            {
                "num_resc_recs",
                (0x18, ">L", 4)},
            {
                "num_wo_placeholders",
                (0x1C, ">L", 4)},
            {
                "offset_to_hrefs",
                (0x20, ">L", 4)},
            {
                "unknown2",
                (0x24, ">L", 4)},
            {
                "title_offset",
                (0x28, ">L", 4)},
            {
                "title_length",
                (0x2C, ">L", 4)}};
        
        public object cont_header_sorted_keys = sortedHeaderKeys(cont_header);
        
        public HdrParser(string header, int start) {
            this.header = header;
            this.start = start;
            this.hdr = new Dictionary<object, object> {
            };
            // set it up for the proper header version
            this.header_sorted_keys = HdrParser.cont_header_sorted_keys;
            this.cont_header = HdrParser.cont_header;
            // parse the header information
            foreach (var key in this.header_sorted_keys) {
                (pos, format_str, _) = this.cont_header[key];
                if (pos < 48) {
                    ValueTuple.Create(val) = @struct.unpack_from(format_str, this.header, pos);
                    this.hdr[key] = val;
                }
            }
            this.exth = this.header[48];
            this.title_offset = this.hdr["title_offset"];
            this.title_length = this.hdr["title_length"];
            this.title = this.header[self.title_offset::(self.title_offset  +  self.title_length)];
            this.codec = "windows-1252";
            this.codec_map = new Dictionary<object, object> {
                {
                    1252,
                    "windows-1252"},
                {
                    65001,
                    "utf-8"}};
            if (this.codec_map.Contains(this.hdr["codepage"])) {
                this.codec = this.codec_map[this.hdr["codepage"]];
            }
            this.title = this.title.decode(this.codec).encode("utf-8");
        }
        
        public virtual object dumpHeaderInfo() {
            object fmt_string;
            foreach (var key in this.cont_header_sorted_keys) {
                (pos, _, tot_len) = this.cont_header[key];
                if (pos < 48) {
                    if (key != "magic") {
                        fmt_string = "  Field: %20s   Offset: 0x%03x   Width:  %d   Value: 0x%0" + tot_len.ToString() + "x";
                    } else {
                        fmt_string = "  Field: %20s   Offset: 0x%03x   Width:  %d   Value: %s";
                    }
                    Console.WriteLine(String.Format(fmt_string, key, pos, tot_len, this.hdr[key]));
                }
            }
            Console.WriteLine(String.Format("\nEXTH Region Length: 0x%0x", this.exth.Count));
            Console.WriteLine("EXTH MetaData: {}".format(this.title.decode("utf-8")));
            dump_contexth(this.codec, this.exth);
        }
    }
    
    public static void main(string infile) {
        Console.WriteLine("DumpAZW6 v01");
        var infileext = Path.GetExtension(infile).ToUpper();
        Console.WriteLine(infile, infileext);
        if (!new List<string> {
            ".AZW6",
            ".RES"
        }.Contains(infileext)) {
            Console.WriteLine("Error: first parameter must be a Kindle AZW6 HD container file with extension .azw6 or .res.");
        } else {
            // make sure it is really an hd container file
            using var stream = File.OpenRead(infile);
            //var reader = new BinaryReader(stream);
            var pdbHead = new PDBHead(stream);
            var palmHeader = new PalmDOCHead(stream);

            var pos = stream.Position;


            var palmheader = contdata[0::78];
            var ident = palmheader[0x3C::(0x3C  +  8)];
            if (ident != new byte[] { (byte)'R', (byte)'B', (byte)'I', (byte)'N', (byte)'C', (byte)'O', (byte)'N', (byte)'T' }) {
                throw new Exception("invalid file format");
            }
            var pp = new PalmDB(contdata);
            var header = pp.readsection(0);
            Console.WriteLine(String.Format("\nFirst Header Dump from Section %d", 0));
            var hp = new HdrParser(header, 0);
            hp.dumpHeaderInfo();
            // now dump a basic sector map of the palmdb
            var n = pp.getnumsections();
            var dtmap = new Dictionary<object, object> {
                {
                    new byte[] { (byte)'F', (byte)'O', (byte)'N', (byte)'T' },
                    "FONT"},
                {
                    new byte[] { (byte)'R', (byte)'E', (byte)'S', (byte)'C' },
                    "RESC"},
                {
                    new byte[] { (byte)'C', (byte)'R', (byte)'E', (byte)'S' },
                    "CRES"},
                {
                    new byte[] { (byte)'C', (byte)'O', (byte)'N', (byte)'T' },
                    "CONT"},
                {
                    new byte[] { 0xA0, 0xA0, 0xA0, 0xA0 },
                    "Empty_Image/Resource_Placeholder"},
                {
                    new byte[] { 0xe9, 0x8e, \\, \n },
                    "EOF_RECORD"}};
            var dtmap2 = new Dictionary<object, object> {
                {
                    "kindle:embed",
                    "KINDLE:EMBED"}};
            hp = null;
            Console.WriteLine("\nMap of Palm DB Sections");
            Console.WriteLine("    Dec  - Hex : Description");
            Console.WriteLine("    ---- - ----  -----------");
            foreach (var i in Enumerable.Range(0, n)) {
                pp.getsecaddr(i);
                var data = pp.readsection(i);
                var dlen = data.Count;
                var dt = data[0::4];
                var dtext = data[0::12];
                var desc = "";
                if (dtmap2.Contains(dtext)) {
                    desc = data;
                    var linkhrefs = new List<object>();
                    var hreflist = desc.split("|");
                    foreach (var href in hreflist) {
                        if (href != "") {
                            linkhrefs.append("        " + href);
                        }
                    }
                    desc = "\n" + "\n".join(linkhrefs);
                } else if (dtmap.Contains(dt)) {
                    desc = dtmap[dt];
                    if (dt == new byte[] { (byte)'C', (byte)'O', (byte)'N', (byte)'T' }) {
                        desc = "Cont Header";
                    } else if (dt == new byte[] { (byte)'C', (byte)'R', (byte)'E', (byte)'S' }) {
                        processCRES(i, data);
                    }
                } else {
                    desc = dtext.hex() + " " + dtext.decode("windows-1252");
                }
                if (desc != "CONT") {
                    Console.WriteLine(String.Format("    %04d - %04x: %s [%d]", i, i, desc, dlen));
                }
            }
        }
    }
    
    public static object parser = argparse.ArgumentParser(description: "Dump the image from an AZW6 HD container file.");
    
    static DumpAZW6_py3() {
        parser.add_argument("infile", type: str, help: "azw6 file to dump the images from");
        main(args.infile);
    }
    
    public static object args = parser.parse_args();
    
    static DumpAZW6_py3() {
        if (@__name__ == "__main__") {
        }
    }
}


using @struct;

using imghdr;

using os;

using sys;

using System.Collections.Generic;

using System;

using System.Linq;

public static class mobimergehdimage {
    
    public static int BHDR_OFFSET_NUM_OF_RECORD = 76;
    
    public static int BHDR_OFFSET_FILE_IDENT = 60;
    
    public static int BHDR_RECORD0_OFFSET_TEXT_ENCODING = 28;
    
    public static int BHDR_RECORD0_OFFSET_FULL_NAME_OFFSET = 84;
    
    public static int BHDR_RECORD0_OFFSET_FULL_NAME_LENGTH = 88;
    
    public static int BHDR_RECORD0_OFFSET_FIRST_IMAGE_INDEX = 108;
    
    public static int BHDR_OFFSET_RECORD_INFO_LIST = 78;
    
    public static int CHDR_OFFSET_FILE_IDENT = 60;
    
    public static int CHDR_RECORD0_OFFSET_TEXT_ENCODING = 12;
    
    public static int CHDR_RECORD0_OFFSET_FULL_NAME_OFFSET = 40;
    
    public static int CHDR_RECORD0_OFFSET_FULL_NAME_LENGTH = 44;
    
    public static Dictionary<int, string> TEXT_ENCODING_MAP = new Dictionary<object, object> {
        {
            65001,
            "UTF-8"},
        {
            1252,
            "windows-1252"}};
    
    public static string CONTAINER_CONTENT_TYPE_IMAGE = "IMAGE";
    
    public static string CONTAINER_CONTENT_TYPE_PLACEHOLDER = "PLACEHOLDER";
    
    public static Dictionary<string, string> CONTAINER_NEEDED_TYPES = new Dictionary<object, object> {
        {
            new byte[] { (byte)'C', (byte)'R', (byte)'E', (byte)'S' },
            CONTAINER_CONTENT_TYPE_IMAGE},
        {
            new byte[] { 0xa0, 0xa0, 0xa0, 0xa0 },
            CONTAINER_CONTENT_TYPE_PLACEHOLDER}};
    
    // Actually, this function is grabbed from 'DumpAZW6'. That script can getting from URL where in header of this code.
    public static void get_image_type(object imgdata) {
        var imgtype = imghdr.what(null, imgdata);
        // horrible hack since imghdr detects jxr/wdp as tiffs
        if (imgtype != null && imgtype == "tiff") {
            imgtype = "wdp";
        }
        // imghdr only checks for JFIF or Exif JPEG files. Apparently, there are some
        // with only the magic JPEG bytes out there...
        // ImageMagick handles those, so, do it too.
        if (imgtype == null) {
            if (imgdata[0::2] == new byte[] { 0xFF, 0xD8 }) {
                // Get last non-null bytes
                var last = imgdata.Count;
                while (imgdata[(last  -  1)::last] == new byte[] { 0x00 }) {
                    last -= 1;
                }
                // Be extra safe, check the trailing bytes, too.
                if (imgdata[(last  -  2)::last] == new byte[] { 0xFF, 0xD9 }) {
                    imgtype = "jpeg";
                }
            }
        }
        return imgtype;
    }
    
    public static string get_charset(object data, int offset) {
        var _tup_1 = @struct.unpack_from(">L", data, offset);
        var charset_id = _tup_1.Item1;
        if (TEXT_ENCODING_MAP.Contains(charset_id)) {
            return TEXT_ENCODING_MAP[charset_id];
        } else {
            Console.WriteLine(String.Format("Unknown Charset %d", charset_id));
            throw;
        }
    }
    
    public static string get_book_title(
        string data,
        object base_offset,
        int name_offset,
        int length_offset,
        string charset) {
        var _tup_1 = @struct.unpack_from(">L", data, base_offset + name_offset);
        name_offset = _tup_1.Item1;
        var _tup_2 = @struct.unpack_from(">L", data, base_offset + length_offset);
        var name_length = _tup_2.Item1;
        name_offset += base_offset;
        return data[name_offset::(name_offset  +  name_length)].decode(charset);
    }
    
    public class MobiMergeHDImage {
        
        public string book_title;
        
        public string charset;
        
        public dict hdimage_dict;
        
        public object mobi;
        
        public object record_dict;
        
        public object hdimage_dict = null;
        
        public MobiMergeHDImage(object mobi_data) {
            this.mobi = bytearray(mobi_data);
            this.record_dict = this.get_record_dict(this.mobi);
            if (this.mobi[BHDR_OFFSET_FILE_IDENT::(BHDR_OFFSET_FILE_IDENT  +  8)] != new byte[] { (byte)'B', (byte)'O', (byte)'O', (byte)'K', (byte)'M', (byte)'O', (byte)'B', (byte)'I' }) {
                Console.WriteLine("This eBook is not a Mobi.");
                throw;
            }
            this.charset = get_charset(this.mobi, this.record_dict[0]["OFFSET"] + BHDR_RECORD0_OFFSET_TEXT_ENCODING);
            this.book_title = get_book_title(this.mobi, this.record_dict[0]["OFFSET"], BHDR_RECORD0_OFFSET_FULL_NAME_OFFSET, BHDR_RECORD0_OFFSET_FULL_NAME_LENGTH, this.charset);
        }
        
        public virtual dict get_record_dict(string data) {
            var current_offset = BHDR_OFFSET_NUM_OF_RECORD;
            var _tup_1 = @struct.unpack_from(">H", data, current_offset);
            var record_count = _tup_1.Item1;
            var record_dict = new dict();
            record_dict[sys.maxsize] = record_count;
            foreach (var index in Enumerable.Range(0, record_count - 0)) {
                record_dict[index] = new dict();
                var offset = BHDR_OFFSET_RECORD_INFO_LIST + index * 8;
                record_dict[index]["INFO_OFFSET"] = offset;
                var _tup_2 = @struct.unpack_from(">L", data, offset);
                record_dict[index]["OFFSET"] = _tup_2.Item1;
            }
            return record_dict;
        }
        
        public virtual void load_azwres(object res_file) {
            using (var azwresfile = open(res_file, "rb")) {
                azwres = azwresfile.read();
            }
            if (azwres[CHDR_OFFSET_FILE_IDENT::(CHDR_OFFSET_FILE_IDENT  +  8)] != new byte[] { (byte)'R', (byte)'B', (byte)'I', (byte)'N', (byte)'C', (byte)'O', (byte)'N', (byte)'T' }) {
                Console.WriteLine(String.Format("%s is not a HDImage container.", os.path.basename(res_file)));
                throw;
            }
            var azwres_dict = this.get_record_dict(azwres);
            var azwres_title = get_book_title(azwres, azwres_dict[0]["OFFSET"], CHDR_RECORD0_OFFSET_FULL_NAME_OFFSET, CHDR_RECORD0_OFFSET_FULL_NAME_LENGTH, this.charset);
            if (this.book_title != azwres_title) {
                Console.WriteLine("Book mismatch. Book title and HDImage container title is not same. ");
                Console.WriteLine(String.Format("Book: %s, Container: %s", this.book_title, azwres_title));
                throw;
            }
            this.hdimage_dict = new dict();
            var image_index = 0;
            foreach (var (index, val) in azwres_dict.items().OrderBy(_p_1 => _p_1).ToList()) {
                if (index != sys.maxsize && azwres[val["OFFSET"]::(val["OFFSET"]  +  2)] != new byte[] { 0xe9, 0x8e }) {
                    if (CONTAINER_NEEDED_TYPES.Contains(azwres[val["OFFSET"]::(val["OFFSET"]  +  4)])) {
                        this.hdimage_dict[image_index] = new dict();
                        this.hdimage_dict[image_index]["INDEX"] = index;
                        this.hdimage_dict[image_index]["TYPE"] = CONTAINER_NEEDED_TYPES[azwres[val["OFFSET"]::(val["OFFSET"]  +  4)]];
                        if (this.hdimage_dict[image_index]["TYPE"] == CONTAINER_CONTENT_TYPE_IMAGE) {
                            this.hdimage_dict[image_index]["CONTENT"] = azwres[(val["OFFSET"]  +  12)::azwres_dict[(index  +  1)]["OFFSET"]];
                        }
                        image_index += 1;
                    }
                }
            }
        }
        
        public virtual void record_offset_update(object record_index, int modified_offset_size) {
            foreach (var target_record_index in Enumerable.Range(record_index + 1, this.record_dict[sys.maxsize] - (record_index + 1))) {
                var newoffset = this.record_dict[target_record_index]["OFFSET"] + modified_offset_size;
                this.record_dict[target_record_index]["OFFSET"] = newoffset;
                var infooffset = this.record_dict[target_record_index]["INFO_OFFSET"];
                this.mobi[infooffset::(infooffset  +  4)] = @struct.pack(">L", newoffset);
            }
        }
        
        public virtual void merge() {
            object img;
            if (this.hdimage_dict == null) {
                Console.WriteLine("'azw.res' file is not loaded yet.");
                throw;
            }
            var first_record_offset = this.record_dict[0]["OFFSET"];
            // Find original book's image
            var _tup_1 = @struct.unpack_from(">L", this.mobi, first_record_offset + 108);
            var first_image_index = _tup_1.Item1;
            var images_dict = new dict();
            var image_index = 0;
            foreach (var record_index in Enumerable.Range(first_image_index, this.record_dict[sys.maxsize] - first_image_index)) {
                if (record_index + 1 == this.record_dict[sys.maxsize]) {
                    img = this.mobi[this.record_dict[record_index]["OFFSET"]];
                } else {
                    img = this.mobi[self.record_dict[record_index]["OFFSET"]::self.record_dict[(record_index  +  1)]["OFFSET"]];
                }
                if (get_image_type(img) != null) {
                    images_dict[image_index] = new dict();
                    images_dict[image_index]["INDEX"] = record_index;
                    images_dict[image_index]["CONTENT"] = img;
                    image_index += 1;
                }
            }
            // Merge
            foreach (var (index, hdimage) in this.hdimage_dict.items().ToList()) {
                if (hdimage["TYPE"] == CONTAINER_CONTENT_TYPE_IMAGE) {
                    // If HDImage type is IMAGE, do merge.
                    if (this.mobi.find(images_dict[index]["CONTENT"]) != -1) {
                        var size_difference = hdimage["CONTENT"].Count - images_dict[index]["CONTENT"].Count;
                        var original_index = images_dict[index]["INDEX"];
                        if (original_index + 1 == this.record_dict[sys.maxsize]) {
                            this.mobi[this.record_dict[original_index]["OFFSET"]] = hdimage["CONTENT"];
                        } else {
                            this.mobi[self.record_dict[original_index]["OFFSET"]::self.record_dict[(original_index  +  1)]["OFFSET"]] = hdimage["CONTENT"];
                        }
                        this.record_offset_update(images_dict[index]["INDEX"], size_difference);
                    }
                } else {
                    // Skip if it's not a image (Maybe PLACEHOLDER)
                }
            }
            return this.mobi;
        }
    }
}

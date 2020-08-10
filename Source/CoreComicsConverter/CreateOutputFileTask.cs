using CoreComicsConverter.Model;
using System.Threading.Tasks;

namespace CoreComicsConverter
{
    public class CreateOutputFileTask : Task
    {
        public Comic Comic { get; private set; }

        public CreateOutputFileTask(Comic comic) : base(() => comic.CreateOutputFile())
        {
            Comic = comic;
        }
    }
}
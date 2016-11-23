using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LZPCompressor
{
    public interface ICompressor
    {
        /// <summary>
        /// Compress input data
        /// </summary>
        /// <exception cref="ArgumentNullException">input is null</exception>
        /// <exception cref="BadInputException">input is too small</exception>
        byte[] Compress(byte[] rawData);

        /// <summary>
        /// Decompress input array
        /// </summary>
        /// <exception cref="ArgumentNullException">input is null</exception>
        /// <exception cref="BadInputException">input is too small</exception>
        /// <exception cref="NotLZP1InputException">input does not correspond implementor's compress format</exception>
        byte[] Decompress(byte[] compessedData);
    }
}

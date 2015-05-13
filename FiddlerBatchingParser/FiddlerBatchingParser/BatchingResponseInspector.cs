using Fiddler;
using System.Text;

namespace Shagman.Codes.FiddlerBatchingParser
{
    public class BatchingResponseInspector: BatchingInspector, IResponseInspector2
    {
        #region IRequestInspector2 Members

        public HTTPResponseHeaders headers
        {
            get
            {
                return _resHeaders;
            }
            set
            {
                _resHeaders = value;

                if (_resHeaders != null)
                {
                    RenderHeader(Encoding.UTF8.GetString(_resHeaders.ToByteArray(false, false)), _resHeaders[ContentType]);
                }
            }
        }

        #endregion
    }
}

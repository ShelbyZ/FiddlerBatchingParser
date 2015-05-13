using Fiddler;
using System.Text;

namespace Shagman.Codes.FiddlerBatchingParser
{
    public class BatchingRequestInspector : BatchingInspector, IRequestInspector2
    {
        #region IResponseInspector2 Members

        public HTTPRequestHeaders headers
        {
            get
            {
                return _reqHeaders;
            }
            set
            {
                _reqHeaders = value;

                if (_reqHeaders != null)
                {
                    RenderHeader(Encoding.UTF8.GetString(_reqHeaders.ToByteArray(false, false, false)), _reqHeaders[ContentType]);
                }
            }
        }

        #endregion
    }
}

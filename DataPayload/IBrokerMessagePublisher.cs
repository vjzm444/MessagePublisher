using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiddleWare.DataStoreConnector.Broker
{
    public interface IBrokerMessagePublisher
    {
        void Publisher<T>(PublisherKey channelName, T payload) where T : class;
    }
}

using System;
using System.Runtime.Serialization;

namespace RvtVa3c
{
  [DataContract]
  class Va3cScene
  {
    [DataMember]
    string Metadata { get; set; }
  }
}

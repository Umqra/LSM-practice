using System.IO;

namespace DataLayer.OperationLog.Operations
{
    //������ ��� ���� ������� �� ������������ ������ � ������� ��������������� �����
    // ����� � ��������� ������� �������� ������

    public class OperationSerializer : IOperationSerializer
    {
        public byte[] Serialize(IOperation operation)
        {
            throw new System.NotImplementedException();
        }

        public IOperation Deserialize(Stream opLogStream)
        {
            throw new System.NotImplementedException();
        }
    }
}
using System;
using Xunit;
using TDengine.Driver;

namespace Function.Test
{
    public class TestTDengineMeta
    {
        /// <author>xiaolei</author>
        /// <Name>TestTDengineMeta.TestTypeNameBool</Name>
        /// <describe>Unit test for object TDengineDriver.TDengineMeta's bool meta info</describe>
        /// <filename>TestTDengineMeta.cs</filename>
        /// <result>pass or failed </result>
        [Fact]
        public void TestTypeNameBool()
        {
            string typeName = "BOOL";
            TDengineMeta meta = new TDengineMeta();
            meta.type = 1;
            string metaTypeName = meta.TypeName();

            Assert.Equal(metaTypeName, typeName);

        }
        /// <author>xiaolei</author>
        /// <Name>TestTDengineMeta.TestTypeNameTINYINT</Name>
        /// <describe>Unit test for object TDengineDriver.TDengineMeta's TinnyInt's meta info</describe>
        /// <filename>TestTDengineMeta.cs</filename>
        /// <result>pass or failed </result>
        [Fact]
        public void TestTypeNameTINYINT()
        {
            string typeName = "TINYINT";
            TDengineMeta meta = new TDengineMeta();
            meta.type = 2;
            string metaTypeName = meta.TypeName();

            Assert.Equal(metaTypeName, typeName);

        }
        /// <author>xiaolei</author>
        /// <Name>TestTDengineMeta.TestTypeNameSMALLINT</Name>
        /// <describe>Unit test for object TDengineDriver.TDengineMeta's SMALLINT's meta info</describe>
        /// <filename>TestTDengineMeta.cs</filename>
        /// <result>pass or failed </result>
        [Fact]
        public void TestTypeNameSMALLINT()
        {
            string typeName = "SMALLINT";
            TDengineMeta meta = new TDengineMeta();
            meta.type = 3;
            string metaTypeName = meta.TypeName();

            Assert.Equal(metaTypeName, typeName);

        }
        /// <author>xiaolei</author>
        /// <Name>TestTDengineMeta.TestTypeNameINT</Name>
        /// <describe>Unit test for object TDengineDriver.TDengineMeta's INT's meta info</describe>
        /// <filename>TestTDengineMeta.cs</filename>
        /// <result>pass or failed </result>
        [Fact]
        public void TestTypeNameINT()
        {
            string typeName = "INT";
            TDengineMeta meta = new TDengineMeta();
            meta.type = 4;
            string metaTypeName = meta.TypeName();

            Assert.Equal(metaTypeName, typeName);

        }
        /// <author>xiaolei</author>
        /// <Name>TestTDengineMeta.TestTypeNameBIGINT</Name>
        /// <describe>Unit test for object TDengineDriver.TDengineMeta's BIGINT's meta info</describe>
        /// <filename>TestTDengineMeta.cs</filename>
        /// <result>pass or failed </result>
        [Fact]
        public void TestTypeNameBIGINT()
        {
            string typeName = "BIGINT";
            TDengineMeta meta = new TDengineMeta();
            meta.type = 5;
            string metaTypeName = meta.TypeName();

            Assert.Equal(metaTypeName, typeName);

        }
        /// <author>xiaolei</author>
        /// <Name>TestTDengineMeta.TestTypeNameUTINYINT</Name>
        /// <describe>Unit test for object TDengineDriver.TDengineMeta's TINYINT UNSIGNED's meta info</describe>
        /// <filename>TestTDengineMeta.cs</filename>
        /// <result>pass or failed </result>
        [Fact]
        public void TestTypeNameUTINYINT()
        {
            string typeName = "TINYINT UNSIGNED";
            TDengineMeta meta = new TDengineMeta();
            meta.type = 11;
            string metaTypeName = meta.TypeName();

            Assert.Equal(metaTypeName, typeName);

        }
        /// <author>xiaolei</author>
        /// <Name>TestTDengineMeta.TestTypeNameUSMALLINT</Name>
        /// <describe>Unit test for object TDengineDriver.TDengineMeta's SMALLINT UNSIGNED's meta info</describe>
        /// <filename>TestTDengineMeta.cs</filename>
        /// <result>pass or failed </result>
        [Fact]
        public void TestTypeNameUSMALLINT()
        {
            string typeName = "SMALLINT UNSIGNED";
            TDengineMeta meta = new TDengineMeta();
            meta.type = 12;
            string metaTypeName = meta.TypeName();

            Assert.Equal(metaTypeName, typeName);

        }
        /// <author>xiaolei</author>
        /// <Name>TestTDengineMeta.TestTypeNameUINT</Name>
        /// <describe>Unit test for object TDengineDriver.TDengineMeta's INT UNSIGNED's meta info</describe>
        /// <filename>TestTDengineMeta.cs</filename>
        /// <result>pass or failed </result>
        [Fact]
        public void TestTypeNameUINT()
        {
            string typeName = "INT UNSIGNED";
            TDengineMeta meta = new TDengineMeta();
            meta.type = 13;
            string metaTypeName = meta.TypeName();

            Assert.Equal(metaTypeName, typeName);

        }
        /// <author>xiaolei</author>
        /// <Name>TestTDengineMeta.TestTypeNameUBIGINT</Name>
        /// <describe>Unit test for object TDengineDriver.TDengineMeta's BIGINT UNSIGNED's meta info</describe>
        /// <filename>TestTDengineMeta.cs</filename>
        /// <result>pass or failed </result>
        [Fact]
        public void TestTypeNameUBIGINT()
        {
            string typeName = "BIGINT UNSIGNED";
            TDengineMeta meta = new TDengineMeta();
            meta.type = 14;
            string metaTypeName = meta.TypeName();

            Assert.Equal(metaTypeName, typeName);

        }
        /// <author>xiaolei</author>
        /// <Name>TestTDengineMeta.TestTypeNameFLOAT</Name>
        /// <describe>Unit test for object TDengineDriver.TDengineMeta's FLOAT's meta info</describe>
        /// <filename>TestTDengineMeta.cs</filename>
        /// <result>pass or failed </result>
        [Fact]
        public void TestTypeNameFLOAT()
        {
            string typeName = "FLOAT";
            TDengineMeta meta = new TDengineMeta();
            meta.type = 6;
            string metaTypeName = meta.TypeName();

            Assert.Equal(metaTypeName, typeName);

        }
        /// <author>xiaolei</author>
        /// <Name>TestTDengineMeta.TestTypeNameDOUBLE</Name>
        /// <describe>Unit test for object TDengineDriver.TDengineMeta's DOUBLE's meta info</describe>
        /// <filename>TestTDengineMeta.cs</filename>
        /// <result>pass or failed </result>
        [Fact]
        public void TestTypeNameDOUBLE()
        {
            string typeName = "DOUBLE";
            TDengineMeta meta = new TDengineMeta();
            meta.type = 7;
            string metaTypeName = meta.TypeName();

            Assert.Equal(metaTypeName, typeName);

        }
        /// <author>xiaolei</author>
        /// <Name>TestTDengineMeta.TestTypeNameSTRING</Name>
        /// <describe>Unit test for object TDengineDriver.TDengineMeta's BINARY's meta info</describe>
        /// <filename>TestTDengineMeta.cs</filename>
        /// <result>pass or failed </result>
        [Fact]
        public void TestTypeNameSTRING()
        {
            string typeName = "BINARY";
            TDengineMeta meta = new TDengineMeta();
            meta.type = 8;
            string metaTypeName = meta.TypeName();

            Assert.Equal(metaTypeName, typeName);

        }
        /// <author>xiaolei</author>
        /// <Name>TestTDengineMeta.TestTypeNameTIMESTAMP</Name>
        /// <describe>Unit test for object TDengineDriver.TDengineMeta's TIMESTAMP's meta info</describe>
        /// <filename>TestTDengineMeta.cs</filename>
        /// <result>pass or failed </result>
        [Fact]
        public void TestTypeNameTIMESTAMP()
        {
            string typeName = "TIMESTAMP";
            TDengineMeta meta = new TDengineMeta();
            meta.type = 9;
            string metaTypeName = meta.TypeName();

            Assert.Equal(metaTypeName, typeName);

        }
        /// <author>xiaolei</author>
        /// <Name>TestTDengineMeta.TestTypeNameNCHAR</Name>
        /// <describe>Unit test for object TDengineDriver.TDengineMeta's NCHAR's meta info</describe>
        /// <filename>TestTDengineMeta.cs</filename>
        /// <result>pass or failed </result>
        [Fact]
        public void TestTypeNameNCHAR()
        {
            string typeName = "NCHAR";
            TDengineMeta meta = new TDengineMeta();
            meta.type = 10;
            string metaTypeName = meta.TypeName();

            Assert.Equal(metaTypeName, typeName);

        }
        /// <author>xiaolei</author>
        /// <Name>TestTDengineMeta.TestTypeNameUndefined</Name>
        /// <describe>Unit test for object TDengineDriver.TDengineMeta's undefine's meta info</describe>
        /// <filename>TestTDengineMeta.cs</filename>
        /// <result>pass or failed </result>
        [Fact]
        public void TestTypeNameUndefined()
        {
            string typeName = "undefine";
            TDengineMeta meta = new TDengineMeta();

            string metaTypeName = meta.TypeName();

            Assert.Equal(metaTypeName, typeName);

        }

    }
}

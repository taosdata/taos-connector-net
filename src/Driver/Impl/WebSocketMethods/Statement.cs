using System;
using TDengine.Driver.Impl.WebSocketMethods.Protocol;

namespace TDengine.Driver.Impl.WebSocketMethods
{
    public partial class Connection
    {
    
        public WSStmtInitResp StmtInit(ulong reqId)
        {
            return SendJsonBackJson<WSStmtInitReq, WSStmtInitResp>(WSAction.STMTInit, new WSStmtInitReq
            {
                ReqId = reqId,
            },reqId);
        }

        public WSStmtPrepareResp StmtPrepare(ulong stmtId,string sql)
        {
            var reqId = _GetReqId();
            return SendJsonBackJson<WSStmtPrepareReq, WSStmtPrepareResp>(WSAction.STMTPrepare, new WSStmtPrepareReq
            {
                ReqId = reqId,
                StmtId = stmtId,
                SQL = sql
            },reqId);
        }
        
        public WSStmtSetTableNameResp StmtSetTableName(ulong stmtId,string tablename)
        {
            var reqId = _GetReqId();
            return SendJsonBackJson<WSStmtSetTableNameReq, WSStmtSetTableNameResp>(WSAction.STMTSetTableName, new WSStmtSetTableNameReq
            {
                ReqId = reqId,
                StmtId = stmtId,
                Name = tablename,
            },reqId);
        }

        public WSStmtSetTagsResp StmtSetTags(ulong stmtId,TaosFieldE[] fields, object[] tags)
        {
            //p0 uin64  req_id
            //p0+8 uint64  stmt_id
            //p0+16 uint64 (1 (set tag) 2 (bind))
            //p0+24 raw block
            Array[] param = new Array[tags.Length];
            for (int i = 0; i < tags.Length; i++)
            {
                if (tags[i] == null)
                {
                    Array newArray = Array.CreateInstance(TDengineConstant.ScanNullableType(fields[i].type), 1);
                    newArray.SetValue(null, 0);
                    param[i] = newArray;
                }
                else
                {
                    Array newArray = Array.CreateInstance(tags[i].GetType(), 1);
                    newArray.SetValue(tags[i], 0);
                    param[i] = newArray;
                }
            }

            var bytes = BlockWriter.Serialize(1, fields, param);
            var req = new byte[24 +bytes.Length];
            var reqId = _GetReqId();
            WriteUInt64ToBytes(req, reqId,0);
            WriteUInt64ToBytes(req,stmtId,8);
            WriteUInt64ToBytes(req,WSActionBinary.SetTagsMessage,16);
            Buffer.BlockCopy(bytes, 0, req, 24, bytes.Length);
            return SendBinaryBackJson<WSStmtSetTagsResp>(req,reqId);
        }
        
        public WSStmtBindResp StmtBind(ulong stmtId,TaosFieldE[] fields, object[] row)
        {
            //p0 uin64  req_id
            //p0+8 uint64  stmt_id
            //p0+16 uint64 (1 (set tag) 2 (bind))
            //p0+24 raw block
            Array[] param = new Array[row.Length];
            for (int i = 0; i < row.Length; i++)
            {
                if (row[i] == null)
                {
                    Array newArray = Array.CreateInstance(TDengineConstant.ScanNullableType(fields[i].type), 1);
                    newArray.SetValue(null, 0);
                    param[i] = newArray;
                }
                else
                {
                    Array newArray = Array.CreateInstance(row[i].GetType(), 1);
                    newArray.SetValue(row[i], 0);
                    param[i] = newArray;
                }
            }

            var bytes = BlockWriter.Serialize(1, fields, param);
            var req = new byte[24 +bytes.Length];
            var reqId = _GetReqId();
            WriteUInt64ToBytes(req, reqId,0);
            WriteUInt64ToBytes(req,stmtId,8);
            WriteUInt64ToBytes(req,WSActionBinary.BindMessage,16);
            Buffer.BlockCopy(bytes, 0, req, 24, bytes.Length);
            return SendBinaryBackJson<WSStmtBindResp>(req,reqId);
        }
        public WSStmtBindResp StmtBind(ulong stmtId,TaosFieldE[] fields, params Array[] param)
        {
            //p0 uin64  req_id
            //p0+8 uint64  stmt_id
            //p0+16 uint64 (1 (set tag) 2 (bind))
            //p0+24 raw block

            var bytes = BlockWriter.Serialize(param[0].Length, fields, param);
            var req = new byte[24 +bytes.Length];
            var reqId = _GetReqId();
            WriteUInt64ToBytes(req, reqId,0);
            WriteUInt64ToBytes(req,stmtId,8);
            WriteUInt64ToBytes(req,WSActionBinary.BindMessage,16);
            Buffer.BlockCopy(bytes, 0, req, 24, bytes.Length);
            return SendBinaryBackJson<WSStmtBindResp>(req,reqId);
        }

        public WSStmtAddBatchResp StmtAddBatch(ulong stmtId)
        {
            var reqId = _GetReqId();
            return SendJsonBackJson<WSStmtAddBatchReq, WSStmtAddBatchResp>(WSAction.STMTAddBatch, new WSStmtAddBatchReq
            {
                ReqId = reqId,
                StmtId = stmtId
            },reqId);
        }
        
        public WSStmtExecResp StmtExec(ulong stmtId)
        {
            var reqId = _GetReqId();
            return SendJsonBackJson<WSStmtExecReq, WSStmtExecResp>(WSAction.STMTExec, new WSStmtExecReq
            {
                ReqId =reqId,
                StmtId = stmtId
            },reqId);
        }

        public WSStmtGetColFieldsResp StmtGetColFields(ulong stmtId)
        {
            var reqId = _GetReqId();
            return SendJsonBackJson<WSStmtGetColFieldsReq, WSStmtGetColFieldsResp>(WSAction.STMTGetColFields, new WSStmtGetColFieldsReq
            {
                ReqId =reqId ,
                StmtId = stmtId
            },reqId);
        }
        public WSStmtGetTagFieldsResp StmtGetTagFields(ulong stmtId)
        {
            var reqId = _GetReqId();
            return SendJsonBackJson<WSStmtGetTagFieldsReq, WSStmtGetTagFieldsResp>(WSAction.STMTGetTagFields, new WSStmtGetTagFieldsReq
            {
                ReqId = reqId,
                StmtId = stmtId
            },reqId);
        }

        public WSStmtUseResultResp StmtUseResult(ulong stmtId)
        {
            var reqId = _GetReqId();
            return SendJsonBackJson<WSStmtUseResultReq, WSStmtUseResultResp>(WSAction.STMTUseResult,
                new WSStmtUseResultReq
                {
                    ReqId = reqId,
                    StmtId = stmtId
                },reqId);
        }
        public void StmtClose(ulong stmtId)
        {
            var reqId = _GetReqId();
            SendJson(WSAction.STMTClose, new WSStmtCloseReq
            {
                ReqId = reqId,
                StmtId = stmtId
            },reqId);
        }
        
    }
}
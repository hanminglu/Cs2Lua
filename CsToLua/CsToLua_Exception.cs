﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Semantics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace RoslynTool.CsToLua
{
    internal partial class CsLuaTranslater
    {
        #region 异常处理

        public override void VisitThrowStatement(ThrowStatementSyntax node)
        {
            CodeBuilder.AppendFormat("{0}luathrow(", GetIndentString());
            IConversionExpression opd = m_Model.GetOperation(node.Expression) as IConversionExpression;
            OutputExpressionSyntax(node.Expression, opd);
            CodeBuilder.AppendLine(");");
        }
        public override void VisitTryStatement(TryStatementSyntax node)
        {
            if (null != node.Block) {
                string retVar = string.Format("__compiler_try_ret_{0}", GetSourcePosForVar(node));
                string errVar = string.Format("__compiler_try_err_{0}", GetSourcePosForVar(node));
                string handledVar = string.Format("__compiler_try_handled_{0}", GetSourcePosForVar(node));

                CodeBuilder.AppendFormat("{0}local {1}, {2} = luatry((function()", GetIndentString(), retVar, errVar);
                CodeBuilder.AppendLine();
                ++m_Indent;
                VisitBlock(node.Block);
                --m_Indent;
                CodeBuilder.AppendFormat("{0}end));", GetIndentString());
                CodeBuilder.AppendLine();

                if (node.Catches.Count > 0) {
                    CodeBuilder.AppendFormat("{0}local {1} = false;", GetIndentString(), handledVar);
                    CodeBuilder.AppendLine();
                    foreach (var clause in node.Catches) {
                        CodeBuilder.AppendFormat("{0}{1} = luacatch({1}, {2}, {3},", GetIndentString(), handledVar, retVar, errVar);
                        CodeBuilder.AppendLine();
                        ++m_Indent;
                        VisitCatchClause(clause);
                        --m_Indent;
                        CodeBuilder.AppendFormat("{0});", GetIndentString());
                        CodeBuilder.AppendLine();
                    }
                    if (node.Catches.Count > 1) {
                        Logger.Instance.Log("Translation Warning", "try have multiple catch ! location: {0}", GetSourcePosForLog(node));
                    }
                }
            }
            if (null != node.Finally) {
                VisitFinallyClause(node.Finally);
            }
        }
        public override void VisitCatchClause(CatchClauseSyntax node)
        {
            string handledVar = string.Format("__compiler_catch_handled_{0}", GetSourcePosForVar(node));
            CodeBuilder.AppendFormat("{0}(function({1}", GetIndentString(), handledVar);
            if (null != node.Declaration) {
                CodeBuilder.Append(", ");
                CodeBuilder.Append(node.Declaration.Identifier.Text);
            }
            CodeBuilder.Append(")");
            CodeBuilder.AppendLine();
            ++m_Indent;
            if (null != node.Filter) {
                CodeBuilder.Append("if ");
                IConversionExpression opd = m_Model.GetOperation(node.Filter.FilterExpression) as IConversionExpression;
                OutputExpressionSyntax(node.Filter.FilterExpression, opd);
                CodeBuilder.Append(" then");
                CodeBuilder.AppendLine();
                ++m_Indent;
            }
            //忽略
            VisitBlock(node.Block);
            CodeBuilder.AppendFormat("{0}{1} = true;", GetIndentString(), handledVar);
            CodeBuilder.AppendLine();
            if (null != node.Filter) {
                --m_Indent;
                CodeBuilder.AppendFormat("{0}end;", GetIndentString());
                CodeBuilder.AppendLine();
            }
            CodeBuilder.AppendFormat("{0}return {1};", GetIndentString(), handledVar);
            CodeBuilder.AppendLine();
            --m_Indent;
            CodeBuilder.AppendFormat("{0}end)", GetIndentString());
            CodeBuilder.AppendLine();
        }
        public override void VisitCatchDeclaration(CatchDeclarationSyntax node)
        {
            //忽略
        }
        public override void VisitCatchFilterClause(CatchFilterClauseSyntax node)
        {
            //忽略
        }
        public override void VisitFinallyClause(FinallyClauseSyntax node)
        {
            CodeBuilder.AppendFormat("{0}do", GetIndentString());
            CodeBuilder.AppendLine();
            ++m_Indent;
            VisitBlock(node.Block);
            --m_Indent;
            CodeBuilder.AppendFormat("{0}end;", GetIndentString());
            CodeBuilder.AppendLine();
        }
        #endregion
    }
}
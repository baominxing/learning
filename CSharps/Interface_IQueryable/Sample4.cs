﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Interface_IQueryable.Sample4
{
    #region Sample4
    public class Sample4
    {
        private static string connectionString = "Data Source=.;Initial Catalog=BTLMaster;user id=sa;password=P@ssw0rd;MultipleActiveResultSets=True;Connect Timeout=120;persist security info=True";

        internal static void Demonstration()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                var code = "Free";

                var stateinfos = new Query<StateInfos>(new DbQueryProvider(connection));

                var states = new Query<States>(new DbQueryProvider(connection));

                var query = from s in states
                            where s.Code == code
                            select new
                            {
                                Code = s.Code,
                                StateInfos = from si in stateinfos
                                             where si.Code == s.Code
                                             select si
                            };


                foreach (var item in query)
                {
                    Console.WriteLine(item.Code);

                    foreach (var item2 in item.StateInfos)
                    {
                        Console.WriteLine(item2.DisplayName);
                    }
                }
            }
        }
    }

    internal sealed class ProjectedColumns
    {
        Expression projector;
        ReadOnlyCollection<ColumnDeclaration> columns;
        internal ProjectedColumns(Expression projector, ReadOnlyCollection<ColumnDeclaration> columns)
        {
            this.projector = projector;
            this.columns = columns;
        }
        internal Expression Projector
        {
            get { return this.projector; }
        }
        internal ReadOnlyCollection<ColumnDeclaration> Columns
        {
            get { return this.columns; }
        }
    }

    internal class ColumnProjector : DbExpressionVisitor
    {
        Nominator nominator;
        Dictionary<ColumnExpression, ColumnExpression> map;
        List<ColumnDeclaration> columns;
        HashSet<string> columnNames;
        HashSet<Expression> candidates;
        string existingAlias;
        string newAlias;
        int iColumn;

        internal ColumnProjector(Func<Expression, bool> fnCanBeColumn)
        {
            this.nominator = new Nominator(fnCanBeColumn);
        }

        internal ProjectedColumns ProjectColumns(Expression expression, string newAlias, string existingAlias)
        {
            this.map = new Dictionary<ColumnExpression, ColumnExpression>();
            this.columns = new List<ColumnDeclaration>();
            this.columnNames = new HashSet<string>();
            this.newAlias = newAlias;
            this.existingAlias = existingAlias;
            this.candidates = this.nominator.Nominate(expression);
            return new ProjectedColumns(this.Visit(expression), this.columns.AsReadOnly());
        }

        protected override Expression Visit(Expression expression)
        {
            if (this.candidates.Contains(expression))
            {
                if (expression.NodeType == (ExpressionType)DbExpressionType.Column)
                {
                    ColumnExpression column = (ColumnExpression)expression;
                    ColumnExpression mapped;
                    if (this.map.TryGetValue(column, out mapped))
                    {
                        return mapped;
                    }
                    if (this.existingAlias == column.Alias)
                    {
                        int ordinal = this.columns.Count;
                        string columnName = this.GetUniqueColumnName(column.Name);
                        this.columns.Add(new ColumnDeclaration(columnName, column));
                        mapped = new ColumnExpression(column.Type, this.newAlias, columnName, ordinal);
                        this.map[column] = mapped;
                        this.columnNames.Add(columnName);
                        return mapped;
                    }
                    // must be referring to outer scope
                    return column;
                }
                else
                {
                    string columnName = this.GetNextColumnName();
                    int ordinal = this.columns.Count;
                    this.columns.Add(new ColumnDeclaration(columnName, expression));
                    return new ColumnExpression(expression.Type, this.newAlias, columnName, ordinal);
                }
            }
            else
            {
                return base.Visit(expression);
            }
        }

        private bool IsColumnNameInUse(string name)
        {
            return this.columnNames.Contains(name);
        }

        private string GetUniqueColumnName(string name)
        {
            string baseName = name;
            int suffix = 1;
            while (this.IsColumnNameInUse(name))
            {
                name = baseName + (suffix++);
            }
            return name;
        }

        private string GetNextColumnName()
        {
            return this.GetUniqueColumnName("c" + (iColumn++));
        }

        class Nominator : DbExpressionVisitor
        {
            Func<Expression, bool> fnCanBeColumn;
            bool isBlocked;
            HashSet<Expression> candidates;

            internal Nominator(Func<Expression, bool> fnCanBeColumn)
            {
                this.fnCanBeColumn = fnCanBeColumn;
            }

            internal HashSet<Expression> Nominate(Expression expression)
            {
                this.candidates = new HashSet<Expression>();
                this.isBlocked = false;
                this.Visit(expression);
                return this.candidates;
            }

            protected override Expression Visit(Expression expression)
            {
                if (expression != null)
                {
                    bool saveIsBlocked = this.isBlocked;
                    this.isBlocked = false;
                    base.Visit(expression);
                    if (!this.isBlocked)
                    {
                        if (this.fnCanBeColumn(expression))
                        {
                            this.candidates.Add(expression);
                        }
                        else
                        {
                            this.isBlocked = true;
                        }
                    }
                    this.isBlocked |= saveIsBlocked;
                }
                return expression;
            }
        }
    }

    internal enum DbExpressionType
    {
        Table = 1000, // make sure these don't overlap with ExpressionType
        Column,
        Select,
        Projection
    }

    internal class TableExpression : Expression
    {
        private string alias;
        private string name;

        internal TableExpression(Type type, string alias, string name)
            : base((ExpressionType)DbExpressionType.Table, type)
        {
            this.alias = alias;
            this.name = name;
        }

        internal string Alias
        {
            get { return this.alias; }
        }

        internal string Name
        {
            get { return this.name; }
        }
    }

    internal class ColumnExpression : Expression
    {
        private string alias;
        private string name;
        private int ordinal;

        internal ColumnExpression(Type type, string alias, string name, int ordinal)
            : base((ExpressionType)DbExpressionType.Column, type)
        {
            this.alias = alias;
            this.name = name;
            this.ordinal = ordinal;
        }

        internal string Alias
        {
            get { return this.alias; }
        }

        internal string Name
        {
            get { return this.name; }
        }

        internal int Ordinal
        {
            get { return this.ordinal; }
        }
    }

    internal class ColumnDeclaration
    {
        private string name;
        private Expression expression;

        internal ColumnDeclaration(string name, Expression expression)
        {
            this.name = name;
            this.expression = expression;
        }

        internal string Name
        {
            get { return this.name; }
        }

        internal Expression Expression
        {
            get { return this.expression; }
        }
    }

    internal class SelectExpression : Expression
    {
        private string alias;
        private ReadOnlyCollection<ColumnDeclaration> columns;
        private Expression from;
        private Expression where;

        internal SelectExpression(Type type, string alias, IEnumerable<ColumnDeclaration> columns, Expression from, Expression where)
            : base((ExpressionType)DbExpressionType.Select, type)
        {
            this.alias = alias;
            this.columns = columns as ReadOnlyCollection<ColumnDeclaration>;
            if (this.columns == null)
            {
                this.columns = new List<ColumnDeclaration>(columns).AsReadOnly();
            }
            this.from = from;
            this.where = where;
        }

        internal string Alias
        {
            get { return this.alias; }
        }

        internal ReadOnlyCollection<ColumnDeclaration> Columns
        {
            get { return this.columns; }
        }

        internal Expression From
        {
            get { return this.from; }
        }

        internal Expression Where
        {
            get { return this.where; }
        }
    }

    internal class ProjectionExpression : Expression
    {
        private SelectExpression source;
        private Expression projector;

        internal ProjectionExpression(SelectExpression source, Expression projector)
            : base((ExpressionType)DbExpressionType.Projection, projector.Type)
        {
            this.source = source;
            this.projector = projector;
        }

        internal SelectExpression Source
        {
            get { return this.source; }
        }

        internal Expression Projector
        {
            get { return this.projector; }
        }
    }

    internal class DbExpressionVisitor : ExpressionVisitor
    {
        protected override Expression Visit(Expression exp)
        {
            if (exp == null)
            {
                return null;
            }
            switch ((DbExpressionType)exp.NodeType)
            {
                case DbExpressionType.Table:
                    return this.VisitTable((TableExpression)exp);

                case DbExpressionType.Column:
                    return this.VisitColumn((ColumnExpression)exp);

                case DbExpressionType.Select:
                    return this.VisitSelect((SelectExpression)exp);

                case DbExpressionType.Projection:
                    return this.VisitProjection((ProjectionExpression)exp);

                default:
                    return base.Visit(exp);
            }
        }

        protected virtual Expression VisitTable(TableExpression table)
        {
            return table;
        }

        protected virtual Expression VisitColumn(ColumnExpression column)
        {
            return column;
        }

        protected virtual Expression VisitSelect(SelectExpression select)
        {
            Expression from = this.VisitSource(select.From);
            Expression where = this.Visit(select.Where);
            ReadOnlyCollection<ColumnDeclaration> columns = this.VisitColumnDeclarations(select.Columns);
            if (from != select.From || where != select.Where || columns != select.Columns)
            {
                return new SelectExpression(select.Type, select.Alias, columns, from, where);
            }
            return select;
        }

        protected virtual Expression VisitSource(Expression source)
        {
            return this.Visit(source);
        }

        protected virtual Expression VisitProjection(ProjectionExpression proj)
        {
            SelectExpression source = (SelectExpression)this.Visit(proj.Source);
            Expression projector = this.Visit(proj.Projector);
            if (source != proj.Source || projector != proj.Projector)
            {
                return new ProjectionExpression(source, projector);
            }
            return proj;
        }

        protected ReadOnlyCollection<ColumnDeclaration> VisitColumnDeclarations(ReadOnlyCollection<ColumnDeclaration> columns)
        {
            List<ColumnDeclaration> alternate = null;
            for (int i = 0, n = columns.Count; i < n; i++)
            {
                ColumnDeclaration column = columns[i];
                Expression e = this.Visit(column.Expression);
                if (alternate == null && e != column.Expression)
                {
                    alternate = columns.Take(i).ToList();
                }
                if (alternate != null)
                {
                    alternate.Add(new ColumnDeclaration(column.Name, e));
                }
            }
            if (alternate != null)
            {
                return alternate.AsReadOnly();
            }
            return columns;
        }
    }

    public class DbQueryProvider : QueryProvider
    {
        DbConnection connection;
        TextWriter log;

        public DbQueryProvider(DbConnection connection)
        {
            this.connection = connection;
        }

        public TextWriter Log
        {
            get { return this.log; }
            set { this.log = value; }
        }

        public override string GetQueryText(Expression expression)
        {
            return this.Translate(expression).CommandText;
        }

        public override object Execute(Expression expression)
        {
            return this.Execute(this.Translate(expression));
        }


        private object Execute(TranslateResult query)
        {
            Delegate projector = query.Projector.Compile();

            if (this.log != null)
            {
                this.log.WriteLine(query.CommandText);
                this.log.WriteLine();
            }

            DbCommand cmd = this.connection.CreateCommand();
            cmd.CommandText = query.CommandText;
            DbDataReader reader = cmd.ExecuteReader();
            Type elementType = TypeSystem.GetElementType(query.Projector.Body.Type);

            return Activator.CreateInstance(
                typeof(ProjectionReader<>).MakeGenericType(elementType),
                BindingFlags.Instance | BindingFlags.NonPublic, null,
                new object[] { reader, projector, this },
                null
                );
        }

        internal class TranslateResult
        {
            internal string CommandText;
            internal LambdaExpression Projector;
        }

        private TranslateResult Translate(Expression expression)
        {
            ProjectionExpression projection = expression as ProjectionExpression;

            if (projection == null)
            {
                expression = Evaluator.PartialEval(expression);
                projection = (ProjectionExpression)new QueryBinder().Bind(expression);
            }

            string commandText = new QueryFormatter().Format(projection.Source);
            LambdaExpression projector = new ProjectionBuilder().Build(projection.Projector, projection.Source.Alias);

            return new TranslateResult { CommandText = commandText, Projector = projector };
        }
    }

    public static class Evaluator
    {
        /// <summary>
        /// Performs evaluation & replacement of independent sub-trees
        /// </summary>
        /// <param name="expression">The root of the expression tree.</param>
        /// <param name="fnCanBeEvaluated">A function that decides whether a given expression node can be part of the local function.</param>
        /// <returns>A new tree with sub-trees evaluated and replaced.</returns>
        public static Expression PartialEval(Expression expression, Func<Expression, bool> fnCanBeEvaluated)
        {
            return new SubtreeEvaluator(new Nominator(fnCanBeEvaluated).Nominate(expression)).Eval(expression);
        }

        /// <summary>
        /// Performs evaluation & replacement of independent sub-trees
        /// </summary>
        /// <param name="expression">The root of the expression tree.</param>
        /// <returns>A new tree with sub-trees evaluated and replaced.</returns>
        public static Expression PartialEval(Expression expression)
        {
            return PartialEval(expression, Evaluator.CanBeEvaluatedLocally);
        }

        private static bool CanBeEvaluatedLocally(Expression expression)
        {
            return expression.NodeType != ExpressionType.Parameter;
        }

        /// <summary>
        /// Evaluates & replaces sub-trees when first candidate is reached (top-down)
        /// </summary>
        class SubtreeEvaluator : ExpressionVisitor
        {
            HashSet<Expression> candidates;

            internal SubtreeEvaluator(HashSet<Expression> candidates)
            {
                this.candidates = candidates;
            }

            internal Expression Eval(Expression exp)
            {
                return this.Visit(exp);
            }

            protected override Expression Visit(Expression exp)
            {
                if (exp == null)
                {
                    return null;
                }
                if (this.candidates.Contains(exp))
                {
                    return this.Evaluate(exp);
                }
                return base.Visit(exp);
            }

            private Expression Evaluate(Expression e)
            {
                if (e.NodeType == ExpressionType.Constant)
                {
                    return e;
                }
                LambdaExpression lambda = Expression.Lambda(e);
                Delegate fn = lambda.Compile();
                return Expression.Constant(fn.DynamicInvoke(null), e.Type);
            }
        }

        /// <summary>
        /// Performs bottom-up analysis to determine which nodes can possibly
        /// be part of an evaluated sub-tree.
        /// </summary>
        class Nominator : ExpressionVisitor
        {
            Func<Expression, bool> fnCanBeEvaluated;
            HashSet<Expression> candidates;
            bool cannotBeEvaluated;

            internal Nominator(Func<Expression, bool> fnCanBeEvaluated)
            {
                this.fnCanBeEvaluated = fnCanBeEvaluated;
            }

            internal HashSet<Expression> Nominate(Expression expression)
            {
                this.candidates = new HashSet<Expression>();
                this.Visit(expression);
                return this.candidates;
            }

            protected override Expression Visit(Expression expression)
            {
                if (expression != null)
                {
                    bool saveCannotBeEvaluated = this.cannotBeEvaluated;
                    this.cannotBeEvaluated = false;
                    base.Visit(expression);
                    if (!this.cannotBeEvaluated)
                    {
                        if (this.fnCanBeEvaluated(expression))
                        {
                            this.candidates.Add(expression);
                        }
                        else
                        {
                            this.cannotBeEvaluated = true;
                        }
                    }
                    this.cannotBeEvaluated |= saveCannotBeEvaluated;
                }
                return expression;
            }
        }
    }

    public abstract class ExpressionVisitor
    {
        protected ExpressionVisitor()
        {
        }

        protected virtual Expression Visit(Expression exp)
        {
            if (exp == null)
                return exp;
            switch (exp.NodeType)
            {
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                case ExpressionType.Not:
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                case ExpressionType.ArrayLength:
                case ExpressionType.Quote:
                case ExpressionType.TypeAs:
                    return this.VisitUnary((UnaryExpression)exp);
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                case ExpressionType.Divide:
                case ExpressionType.Modulo:
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.Coalesce:
                case ExpressionType.ArrayIndex:
                case ExpressionType.RightShift:
                case ExpressionType.LeftShift:
                case ExpressionType.ExclusiveOr:
                    return this.VisitBinary((BinaryExpression)exp);
                case ExpressionType.TypeIs:
                    return this.VisitTypeIs((TypeBinaryExpression)exp);
                case ExpressionType.Conditional:
                    return this.VisitConditional((ConditionalExpression)exp);
                case ExpressionType.Constant:
                    return this.VisitConstant((ConstantExpression)exp);
                case ExpressionType.Parameter:
                    return this.VisitParameter((ParameterExpression)exp);
                case ExpressionType.MemberAccess:
                    return this.VisitMemberAccess((MemberExpression)exp);
                case ExpressionType.Call:
                    return this.VisitMethodCall((MethodCallExpression)exp);
                case ExpressionType.Lambda:
                    return this.VisitLambda((LambdaExpression)exp);
                case ExpressionType.New:
                    return this.VisitNew((NewExpression)exp);
                case ExpressionType.NewArrayInit:
                case ExpressionType.NewArrayBounds:
                    return this.VisitNewArray((NewArrayExpression)exp);
                case ExpressionType.Invoke:
                    return this.VisitInvocation((InvocationExpression)exp);
                case ExpressionType.MemberInit:
                    return this.VisitMemberInit((MemberInitExpression)exp);
                case ExpressionType.ListInit:
                    return this.VisitListInit((ListInitExpression)exp);
                default:
                    throw new Exception(string.Format("Unhandled expression type: '{0}'", exp.NodeType));
            }
        }

        protected virtual MemberBinding VisitBinding(MemberBinding binding)
        {
            switch (binding.BindingType)
            {
                case MemberBindingType.Assignment:
                    return this.VisitMemberAssignment((MemberAssignment)binding);
                case MemberBindingType.MemberBinding:
                    return this.VisitMemberMemberBinding((MemberMemberBinding)binding);
                case MemberBindingType.ListBinding:
                    return this.VisitMemberListBinding((MemberListBinding)binding);
                default:
                    throw new Exception(string.Format("Unhandled binding type '{0}'", binding.BindingType));
            }
        }

        protected virtual ElementInit VisitElementInitializer(ElementInit initializer)
        {
            ReadOnlyCollection<Expression> arguments = this.VisitExpressionList(initializer.Arguments);
            if (arguments != initializer.Arguments)
            {
                return Expression.ElementInit(initializer.AddMethod, arguments);
            }
            return initializer;
        }

        protected virtual Expression VisitUnary(UnaryExpression u)
        {
            Expression operand = this.Visit(u.Operand);
            if (operand != u.Operand)
            {
                return Expression.MakeUnary(u.NodeType, operand, u.Type, u.Method);
            }
            return u;
        }

        protected virtual Expression VisitBinary(BinaryExpression b)
        {
            Expression left = this.Visit(b.Left);
            Expression right = this.Visit(b.Right);
            Expression conversion = this.Visit(b.Conversion);
            if (left != b.Left || right != b.Right || conversion != b.Conversion)
            {
                if (b.NodeType == ExpressionType.Coalesce && b.Conversion != null)
                    return Expression.Coalesce(left, right, conversion as LambdaExpression);
                else
                    return Expression.MakeBinary(b.NodeType, left, right, b.IsLiftedToNull, b.Method);
            }
            return b;
        }

        protected virtual Expression VisitTypeIs(TypeBinaryExpression b)
        {
            Expression expr = this.Visit(b.Expression);
            if (expr != b.Expression)
            {
                return Expression.TypeIs(expr, b.TypeOperand);
            }
            return b;
        }

        protected virtual Expression VisitConstant(ConstantExpression c)
        {
            return c;
        }

        protected virtual Expression VisitConditional(ConditionalExpression c)
        {
            Expression test = this.Visit(c.Test);
            Expression ifTrue = this.Visit(c.IfTrue);
            Expression ifFalse = this.Visit(c.IfFalse);
            if (test != c.Test || ifTrue != c.IfTrue || ifFalse != c.IfFalse)
            {
                return Expression.Condition(test, ifTrue, ifFalse);
            }
            return c;
        }

        protected virtual Expression VisitParameter(ParameterExpression p)
        {
            return p;
        }

        protected virtual Expression VisitMemberAccess(MemberExpression m)
        {
            Expression exp = this.Visit(m.Expression);
            if (exp != m.Expression)
            {
                return Expression.MakeMemberAccess(exp, m.Member);
            }
            return m;
        }

        protected virtual Expression VisitMethodCall(MethodCallExpression m)
        {
            Expression obj = this.Visit(m.Object);
            IEnumerable<Expression> args = this.VisitExpressionList(m.Arguments);
            if (obj != m.Object || args != m.Arguments)
            {
                return Expression.Call(obj, m.Method, args);
            }
            return m;
        }

        protected virtual ReadOnlyCollection<Expression> VisitExpressionList(ReadOnlyCollection<Expression> original)
        {
            List<Expression> list = null;
            for (int i = 0, n = original.Count; i < n; i++)
            {
                Expression p = this.Visit(original[i]);
                if (list != null)
                {
                    list.Add(p);
                }
                else if (p != original[i])
                {
                    list = new List<Expression>(n);
                    for (int j = 0; j < i; j++)
                    {
                        list.Add(original[j]);
                    }
                    list.Add(p);
                }
            }
            if (list != null)
            {
                return list.AsReadOnly();
            }
            return original;
        }

        protected virtual MemberAssignment VisitMemberAssignment(MemberAssignment assignment)
        {
            Expression e = this.Visit(assignment.Expression);
            if (e != assignment.Expression)
            {
                return Expression.Bind(assignment.Member, e);
            }
            return assignment;
        }

        protected virtual MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding binding)
        {
            IEnumerable<MemberBinding> bindings = this.VisitBindingList(binding.Bindings);
            if (bindings != binding.Bindings)
            {
                return Expression.MemberBind(binding.Member, bindings);
            }
            return binding;
        }

        protected virtual MemberListBinding VisitMemberListBinding(MemberListBinding binding)
        {
            IEnumerable<ElementInit> initializers = this.VisitElementInitializerList(binding.Initializers);
            if (initializers != binding.Initializers)
            {
                return Expression.ListBind(binding.Member, initializers);
            }
            return binding;
        }

        protected virtual IEnumerable<MemberBinding> VisitBindingList(ReadOnlyCollection<MemberBinding> original)
        {
            List<MemberBinding> list = null;
            for (int i = 0, n = original.Count; i < n; i++)
            {
                MemberBinding b = this.VisitBinding(original[i]);
                if (list != null)
                {
                    list.Add(b);
                }
                else if (b != original[i])
                {
                    list = new List<MemberBinding>(n);
                    for (int j = 0; j < i; j++)
                    {
                        list.Add(original[j]);
                    }
                    list.Add(b);
                }
            }
            if (list != null)
                return list;
            return original;
        }

        protected virtual IEnumerable<ElementInit> VisitElementInitializerList(ReadOnlyCollection<ElementInit> original)
        {
            List<ElementInit> list = null;
            for (int i = 0, n = original.Count; i < n; i++)
            {
                ElementInit init = this.VisitElementInitializer(original[i]);
                if (list != null)
                {
                    list.Add(init);
                }
                else if (init != original[i])
                {
                    list = new List<ElementInit>(n);
                    for (int j = 0; j < i; j++)
                    {
                        list.Add(original[j]);
                    }
                    list.Add(init);
                }
            }
            if (list != null)
                return list;
            return original;
        }

        protected virtual Expression VisitLambda(LambdaExpression lambda)
        {
            Expression body = this.Visit(lambda.Body);
            if (body != lambda.Body)
            {
                return Expression.Lambda(lambda.Type, body, lambda.Parameters);
            }
            return lambda;
        }

        protected virtual NewExpression VisitNew(NewExpression nex)
        {
            IEnumerable<Expression> args = this.VisitExpressionList(nex.Arguments);
            if (args != nex.Arguments)
            {
                if (nex.Members != null)
                    return Expression.New(nex.Constructor, args, nex.Members);
                else
                    return Expression.New(nex.Constructor, args);
            }
            return nex;
        }

        protected virtual Expression VisitMemberInit(MemberInitExpression init)
        {
            NewExpression n = this.VisitNew(init.NewExpression);
            IEnumerable<MemberBinding> bindings = this.VisitBindingList(init.Bindings);
            if (n != init.NewExpression || bindings != init.Bindings)
            {
                return Expression.MemberInit(n, bindings);
            }
            return init;
        }

        protected virtual Expression VisitListInit(ListInitExpression init)
        {
            NewExpression n = this.VisitNew(init.NewExpression);
            IEnumerable<ElementInit> initializers = this.VisitElementInitializerList(init.Initializers);
            if (n != init.NewExpression || initializers != init.Initializers)
            {
                return Expression.ListInit(n, initializers);
            }
            return init;
        }

        protected virtual Expression VisitNewArray(NewArrayExpression na)
        {
            IEnumerable<Expression> exprs = this.VisitExpressionList(na.Expressions);
            if (exprs != na.Expressions)
            {
                if (na.NodeType == ExpressionType.NewArrayInit)
                {
                    return Expression.NewArrayInit(na.Type.GetElementType(), exprs);
                }
                else
                {
                    return Expression.NewArrayBounds(na.Type.GetElementType(), exprs);
                }
            }
            return na;
        }

        protected virtual Expression VisitInvocation(InvocationExpression iv)
        {
            IEnumerable<Expression> args = this.VisitExpressionList(iv.Arguments);
            Expression expr = this.Visit(iv.Expression);
            if (args != iv.Arguments || expr != iv.Expression)
            {
                return Expression.Invoke(expr, args);
            }
            return iv;
        }
    }

    internal class ProjectionBuilder : DbExpressionVisitor
    {
        ParameterExpression row;
        string rowAlias;
        static MethodInfo miGetValue;
        static MethodInfo miExecuteSubQuery;

        internal ProjectionBuilder()
        {
            if (miGetValue == null)
            {
                miGetValue = typeof(ProjectionRow).GetMethod("GetValue");
                miExecuteSubQuery = typeof(ProjectionRow).GetMethod("ExecuteSubQuery");

            }
        }

        internal LambdaExpression Build(Expression expression, string alias)
        {
            this.row = Expression.Parameter(typeof(ProjectionRow), "row");
            this.rowAlias = alias;
            Expression body = this.Visit(expression);
            return Expression.Lambda(body, this.row);
        }


        protected override Expression VisitColumn(ColumnExpression column)
        {
            if (column.Alias == this.rowAlias)
            {
                return Expression.Convert(Expression.Call(this.row, miGetValue, Expression.Constant(column.Ordinal)), column.Type);
            }
            return column;
        }

        protected override Expression VisitProjection(ProjectionExpression proj)
        {
            LambdaExpression subQuery = Expression.Lambda(base.VisitProjection(proj), this.row);
            Type elementType = TypeSystem.GetElementType(subQuery.Body.Type);
            MethodInfo mi = miExecuteSubQuery.MakeGenericMethod(elementType);
            return Expression.Convert(
                Expression.Call(this.row, mi, Expression.Constant(subQuery)),
                proj.Type
                );
        }

    }

    public abstract class ProjectionRow
    {
        public abstract object GetValue(int index);

        public abstract IEnumerable<E> ExecuteSubQuery<E>(LambdaExpression query);
    }

    internal class ProjectionReader<T> : IEnumerable<T>, IEnumerable
    {
        Enumerator enumerator;

        internal ProjectionReader(DbDataReader reader, Func<ProjectionRow, T> projector, IQueryProvider provider)
        {
            this.enumerator = new Enumerator(reader, projector, provider);
        }


        public IEnumerator<T> GetEnumerator()
        {
            Enumerator e = this.enumerator;
            if (e == null)
            {
                throw new InvalidOperationException("Cannot enumerate more than once");
            }
            this.enumerator = null;
            return e;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        class Enumerator : ProjectionRow, IEnumerator<T>, IEnumerator, IDisposable
        {
            DbDataReader reader;
            T current;
            Func<ProjectionRow, T> projector;
            IQueryProvider provider;

            internal Enumerator(DbDataReader reader, Func<ProjectionRow, T> projector, IQueryProvider provider)
            {
                this.reader = reader;
                this.projector = projector;
                this.provider = provider;
            }


            public override object GetValue(int index)
            {
                if (index >= 0)
                {
                    if (this.reader.IsDBNull(index))
                    {
                        return null;
                    }
                    else
                    {
                        return this.reader.GetValue(index);
                    }
                }
                throw new IndexOutOfRangeException();
            }

            public override IEnumerable<E> ExecuteSubQuery<E>(LambdaExpression query)
            {
                ProjectionExpression projection = (ProjectionExpression)new Replacer().Replace(query.Body, query.Parameters[0], Expression.Constant(this));
                projection = (ProjectionExpression)Evaluator.PartialEval(projection, CanEvaluateLocally);
                IEnumerable<E> result = (IEnumerable<E>)this.provider.Execute(projection);
                List<E> list = new List<E>(result);
                if (typeof(IQueryable<E>).IsAssignableFrom(query.Body.Type))
                {
                    return list.AsQueryable();
                }
                return list;
            }

            private static bool CanEvaluateLocally(Expression expression)
            {
                if (expression.NodeType == ExpressionType.Parameter ||
                    expression.NodeType.IsDbExpression())
                {
                    return false;
                }
                return true;
            }



            public T Current
            {
                get { return this.current; }
            }

            object IEnumerator.Current
            {
                get { return this.current; }
            }

            public bool MoveNext()
            {
                if (this.reader.Read())
                {
                    this.current = this.projector(this);
                    return true;
                }
                return false;
            }

            public void Reset()
            {
            }

            public void Dispose()
            {
                this.reader.Dispose();
            }
        }
    }

    internal class Replacer : DbExpressionVisitor
    {
        Expression searchFor;
        Expression replaceWith;
        internal Expression Replace(Expression expression, Expression searchFor, Expression replaceWith)
        {
            this.searchFor = searchFor;
            this.replaceWith = replaceWith;
            return this.Visit(expression);
        }
        protected override Expression Visit(Expression exp)
        {
            if (exp == this.searchFor)
            {
                return this.replaceWith;
            }
            return base.Visit(exp);
        }
    }

    internal class QueryBinder : ExpressionVisitor
    {
        private ColumnProjector columnProjector;
        private Dictionary<ParameterExpression, Expression> map;
        private int aliasCount;

        internal QueryBinder()
        {
            this.columnProjector = new ColumnProjector(this.CanBeColumn);
        }

        private bool CanBeColumn(Expression expression)
        {
            return expression.NodeType == (ExpressionType)DbExpressionType.Column;
        }

        internal Expression Bind(Expression expression)
        {
            this.map = new Dictionary<ParameterExpression, Expression>();
            return this.Visit(expression);
        }

        private static Expression StripQuotes(Expression e)
        {
            while (e.NodeType == ExpressionType.Quote)
            {
                e = ((UnaryExpression)e).Operand;
            }
            return e;
        }

        private string GetNextAlias()
        {
            return "t" + (aliasCount++);
        }

        private ProjectedColumns ProjectColumns(Expression expression, string newAlias, string existingAlias)
        {
            return this.columnProjector.ProjectColumns(expression, newAlias, existingAlias);
        }

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            if (m.Method.DeclaringType == typeof(Queryable) ||
                m.Method.DeclaringType == typeof(Enumerable))
            {
                switch (m.Method.Name)
                {
                    case "Where":
                        return this.BindWhere(m.Type, m.Arguments[0], (LambdaExpression)StripQuotes(m.Arguments[1]));

                    case "Select":
                        return this.BindSelect(m.Type, m.Arguments[0], (LambdaExpression)StripQuotes(m.Arguments[1]));
                }
                throw new NotSupportedException(string.Format("The method '{0}' is not supported", m.Method.Name));
            }
            return base.VisitMethodCall(m);
        }

        private Expression BindWhere(Type resultType, Expression source, LambdaExpression predicate)
        {
            ProjectionExpression projection = (ProjectionExpression)this.Visit(source);
            this.map[predicate.Parameters[0]] = projection.Projector;
            Expression where = this.Visit(predicate.Body);
            string alias = this.GetNextAlias();
            ProjectedColumns pc = this.ProjectColumns(projection.Projector, alias, GetExistingAlias(projection.Source));
            return new ProjectionExpression(
                new SelectExpression(resultType, alias, pc.Columns, projection.Source, where),
                pc.Projector
                );
        }

        private Expression BindSelect(Type resultType, Expression source, LambdaExpression selector)
        {
            ProjectionExpression projection = (ProjectionExpression)this.Visit(source);
            this.map[selector.Parameters[0]] = projection.Projector;
            Expression expression = this.Visit(selector.Body);
            string alias = this.GetNextAlias();
            ProjectedColumns pc = this.ProjectColumns(expression, alias, GetExistingAlias(projection.Source));
            return new ProjectionExpression(
                new SelectExpression(resultType, alias, pc.Columns, projection.Source, null),
                pc.Projector
                );
        }

        private static string GetExistingAlias(Expression source)
        {
            switch ((DbExpressionType)source.NodeType)
            {
                case DbExpressionType.Select:
                    return ((SelectExpression)source).Alias;

                case DbExpressionType.Table:
                    return ((TableExpression)source).Alias;

                default:
                    throw new InvalidOperationException(string.Format("Invalid source node type '{0}'", source.NodeType));
            }
        }

        private bool IsTable(object value)
        {
            IQueryable q = value as IQueryable;
            return q != null && q.Expression.NodeType == ExpressionType.Constant;
        }

        private string GetTableName(object table)
        {
            IQueryable tableQuery = (IQueryable)table;
            Type rowType = tableQuery.ElementType;
            return rowType.Name;
        }

        private string GetColumnName(MemberInfo member)
        {
            return member.Name;
        }

        private Type GetColumnType(MemberInfo member)
        {
            FieldInfo fi = member as FieldInfo;
            if (fi != null)
            {
                return fi.FieldType;
            }
            PropertyInfo pi = (PropertyInfo)member;
            return pi.PropertyType;
        }

        private IEnumerable<MemberInfo> GetMappedMembers(Type rowType)
        {
            return rowType.GetProperties().Cast<MemberInfo>();
        }

        private ProjectionExpression GetTableProjection(object value)
        {
            IQueryable table = (IQueryable)value;
            string tableAlias = this.GetNextAlias();
            string selectAlias = this.GetNextAlias();
            List<MemberBinding> bindings = new List<MemberBinding>();
            List<ColumnDeclaration> columns = new List<ColumnDeclaration>();
            foreach (MemberInfo mi in this.GetMappedMembers(table.ElementType))
            {
                string columnName = this.GetColumnName(mi);
                Type columnType = this.GetColumnType(mi);
                int ordinal = columns.Count;
                bindings.Add(Expression.Bind(mi, new ColumnExpression(columnType, selectAlias, columnName, ordinal)));
                columns.Add(new ColumnDeclaration(columnName, new ColumnExpression(columnType, tableAlias, columnName, ordinal)));
            }
            Expression projector = Expression.MemberInit(Expression.New(table.ElementType), bindings);
            Type resultType = typeof(IEnumerable<>).MakeGenericType(table.ElementType);
            return new ProjectionExpression(
                new SelectExpression(
                    resultType,
                    selectAlias,
                    columns,
                    new TableExpression(resultType, tableAlias, this.GetTableName(table)),
                    null
                    ),
                projector
                );
        }

        protected override Expression VisitConstant(ConstantExpression c)
        {
            if (this.IsTable(c.Value))
            {
                return GetTableProjection(c.Value);
            }
            return c;
        }

        protected override Expression VisitParameter(ParameterExpression p)
        {
            Expression e;
            if (this.map.TryGetValue(p, out e))
            {
                return e;
            }
            return p;
        }

        protected override Expression VisitMemberAccess(MemberExpression m)
        {
            Expression source = this.Visit(m.Expression);
            switch (source.NodeType)
            {
                case ExpressionType.MemberInit:
                    MemberInitExpression min = (MemberInitExpression)source;
                    for (int i = 0, n = min.Bindings.Count; i < n; i++)
                    {
                        MemberAssignment assign = min.Bindings[i] as MemberAssignment;
                        if (assign != null && MembersMatch(assign.Member, m.Member))
                        {
                            return assign.Expression;
                        }
                    }
                    break;

                case ExpressionType.New:
                    NewExpression nex = (NewExpression)source;
                    if (nex.Members != null)
                    {
                        for (int i = 0, n = nex.Members.Count; i < n; i++)
                        {
                            if (MembersMatch(nex.Members[i], m.Member))
                            {
                                return nex.Arguments[i];
                            }
                        }
                    }
                    break;
            }
            if (source == m.Expression)
            {
                return m;
            }
            return MakeMemberAccess(source, m.Member);
        }

        private bool MembersMatch(MemberInfo a, MemberInfo b)
        {
            if (a == b)
            {
                return true;
            }
            if (a is MethodInfo && b is PropertyInfo)
            {
                return a == ((PropertyInfo)b).GetGetMethod();
            }
            else if (a is PropertyInfo && b is MethodInfo)
            {
                return ((PropertyInfo)a).GetGetMethod() == b;
            }
            return false;
        }

        private Expression MakeMemberAccess(Expression source, MemberInfo mi)
        {
            FieldInfo fi = mi as FieldInfo;
            if (fi != null)
            {
                return Expression.Field(source, fi);
            }
            PropertyInfo pi = (PropertyInfo)mi;
            return Expression.Property(source, pi);
        }
    }

    internal class QueryFormatter : DbExpressionVisitor
    {
        StringBuilder sb;
        int indent = 2;
        int depth;

        internal QueryFormatter()
        {
        }

        internal string Format(Expression expression)
        {
            this.sb = new StringBuilder();
            this.Visit(expression);
            return this.sb.ToString();
        }

        protected enum Identation
        {
            Same,
            Inner,
            Outer
        }

        internal int IdentationWidth
        {
            get { return this.indent; }
            set { this.indent = value; }
        }

        private void AppendNewLine(Identation style)
        {
            sb.AppendLine();
            if (style == Identation.Inner)
            {
                this.depth++;
            }
            else if (style == Identation.Outer)
            {
                this.depth--;
                System.Diagnostics.Debug.Assert(this.depth >= 0);
            }
            for (int i = 0, n = this.depth * this.indent; i < n; i++)
            {
                sb.Append(" ");
            }
        }

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            throw new NotSupportedException(string.Format("The method '{0}' is not supported", m.Method.Name));
        }

        protected override Expression VisitUnary(UnaryExpression u)
        {
            switch (u.NodeType)
            {
                case ExpressionType.Not:
                    sb.Append(" NOT ");
                    this.Visit(u.Operand);
                    break;
                default:
                    throw new NotSupportedException(string.Format("The unary operator '{0}' is not supported", u.NodeType));
            }
            return u;
        }

        protected override Expression VisitBinary(BinaryExpression b)
        {
            sb.Append("(");
            this.Visit(b.Left);
            switch (b.NodeType)
            {
                case ExpressionType.And:
                    sb.Append(" AND ");
                    break;
                case ExpressionType.Or:
                    sb.Append(" OR");
                    break;
                case ExpressionType.Equal:
                    sb.Append(" = ");
                    break;
                case ExpressionType.NotEqual:
                    sb.Append(" <> ");
                    break;
                case ExpressionType.LessThan:
                    sb.Append(" < ");
                    break;
                case ExpressionType.LessThanOrEqual:
                    sb.Append(" <= ");
                    break;
                case ExpressionType.GreaterThan:
                    sb.Append(" > ");
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    sb.Append(" >= ");
                    break;
                default:
                    throw new NotSupportedException(string.Format("The binary operator '{0}' is not supported", b.NodeType));
            }
            this.Visit(b.Right);
            sb.Append(")");
            return b;
        }

        protected override Expression VisitConstant(ConstantExpression c)
        {
            if (c.Value == null)
            {
                sb.Append("NULL");
            }
            else
            {
                switch (Type.GetTypeCode(c.Value.GetType()))
                {
                    case TypeCode.Boolean:
                        sb.Append(((bool)c.Value) ? 1 : 0);
                        break;
                    case TypeCode.String:
                        sb.Append("'");
                        sb.Append(c.Value);
                        sb.Append("'");
                        break;
                    case TypeCode.Object:
                        throw new NotSupportedException(string.Format("The constant for '{0}' is not supported", c.Value));
                    default:
                        sb.Append(c.Value);
                        break;
                }
            }
            return c;
        }

        protected override Expression VisitColumn(ColumnExpression column)
        {
            if (!string.IsNullOrEmpty(column.Alias))
            {
                sb.Append(column.Alias);
                sb.Append(".");
            }
            sb.Append(column.Name);
            return column;
        }

        protected override Expression VisitSelect(SelectExpression select)
        {
            sb.Append("SELECT ");
            for (int i = 0, n = select.Columns.Count; i < n; i++)
            {
                ColumnDeclaration column = select.Columns[i];
                if (i > 0)
                {
                    sb.Append(", ");
                }
                ColumnExpression c = this.Visit(column.Expression) as ColumnExpression;
                if (c == null || c.Name != select.Columns[i].Name)
                {
                    sb.Append(" AS ");
                    sb.Append(column.Name);
                }
            }
            if (select.From != null)
            {
                this.AppendNewLine(Identation.Same);
                sb.Append("FROM ");
                this.VisitSource(select.From);
            }
            if (select.Where != null)
            {
                this.AppendNewLine(Identation.Same);
                sb.Append("WHERE ");
                this.Visit(select.Where);
            }
            return select;
        }

        protected override Expression VisitSource(Expression source)
        {
            switch ((DbExpressionType)source.NodeType)
            {
                case DbExpressionType.Table:
                    TableExpression table = (TableExpression)source;
                    sb.Append(table.Name);
                    sb.Append(" AS ");
                    sb.Append(table.Alias);
                    break;
                case DbExpressionType.Select:
                    SelectExpression select = (SelectExpression)source;
                    sb.Append("(");
                    this.AppendNewLine(Identation.Inner);
                    this.Visit(select);
                    this.AppendNewLine(Identation.Outer);
                    sb.Append(")");
                    sb.Append(" AS ");
                    sb.Append(select.Alias);
                    break;
                default:
                    throw new InvalidOperationException("Select source is not valid type");
            }
            return source;
        }
    }

    public abstract class QueryProvider : IQueryProvider
    {
        protected QueryProvider()
        {
        }

        IQueryable<S> IQueryProvider.CreateQuery<S>(Expression expression)
        {
            return new Query<S>(this, expression);
        }

        IQueryable IQueryProvider.CreateQuery(Expression expression)
        {
            Type elementType = TypeSystem.GetElementType(expression.Type);
            try
            {
                return (IQueryable)Activator.CreateInstance(typeof(Query<>).MakeGenericType(elementType), new object[] { this, expression });
            }
            catch (TargetInvocationException tie)
            {
                throw tie.InnerException;
            }
        }

        S IQueryProvider.Execute<S>(Expression expression)
        {
            return (S)this.Execute(expression);
        }

        object IQueryProvider.Execute(Expression expression)
        {
            return this.Execute(expression);
        }

        public abstract string GetQueryText(Expression expression);
        public abstract object Execute(Expression expression);
    }

    public class Query<T> : IQueryable<T>, IQueryable, IEnumerable<T>, IEnumerable, IOrderedQueryable<T>, IOrderedQueryable
    {
        QueryProvider provider;
        Expression expression;

        public Query(QueryProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException("provider");
            }
            this.provider = provider;
            this.expression = Expression.Constant(this);
        }

        public Query(QueryProvider provider, Expression expression)
        {
            if (provider == null)
            {
                throw new ArgumentNullException("provider");
            }
            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }
            if (!typeof(IQueryable<T>).IsAssignableFrom(expression.Type))
            {
                throw new ArgumentOutOfRangeException("expression");
            }
            this.provider = provider;
            this.expression = expression;
        }

        Expression IQueryable.Expression
        {
            get { return this.expression; }
        }

        Type IQueryable.ElementType
        {
            get { return typeof(T); }
        }

        IQueryProvider IQueryable.Provider
        {
            get { return this.provider; }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)this.provider.Execute(this.expression)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)this.provider.Execute(this.expression)).GetEnumerator();
        }

        public override string ToString()
        {
            return this.provider.GetQueryText(this.expression);
        }
    }

    public static class DbExpressionExtensions
    {
        public static bool IsDbExpression(this ExpressionType nodeType)
        {
            return (int)nodeType >= (int)DbExpressionType.Table;
        }
    }
    #endregion Sample3
}
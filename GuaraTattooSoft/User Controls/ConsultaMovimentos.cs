﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GuaraTattooSoft.Entidades;
using GuaraTattooSoft.Extencoes;
using System.Threading;
using GuaraTattooSoft.Forms;
using GuaraTattooSoft.Relatorios.DataSets;
using Microsoft.Reporting.WinForms;
using GuaraTattooSoft.Relatorios;

namespace GuaraTattooSoft.User_Controls
{
    public partial class ConsultaMovimentos : UserControl
    {

        Movimentos mov = null;
        Thread tarefa;

        DataTable dtTmv = new DataTable();
        public ConsultaMovimentos()
        {
            InitializeComponent();

            dataGridMovimentos.AplicarPadroes();
            this.AplicarPadroes();
            
            if (cbExibir.Text == "Apenas este mês")
            {
                mov = new Movimentos();
                mov.Pesquisar("id", "%", 1);
            }

            dtTmv.Columns.Add("id", typeof(int));
            dtTmv.Columns.Add("descricao", typeof(string));
            dtTmv.Columns.Add("total", typeof(decimal));

            AtualizaDataGrid();
        }

        //private void ExecuteThread()
        //{
        //    tarefa = new Thread(AtualizaDataGrid);
        //    if (cbExibir.Text == "Apenas este mês")
        //    {
        //        mov = new Movimentos();
        //        mov.Pesquisar("id", "%", 1);
        //    }
        //    tarefa.Start();
        //}

        private void AtualizaDataGrid()
        {
            if (mov == null) mov = new Movimentos(true);

            cbExibir.Enabled = false;
            lbTotal.Text = "0,00";
            dataGridMovimentos.Rows.Clear();
            notif.Text = "Aguarde...";
            dtTmv.Rows.Clear();

            for (int i = 0; i < mov.id_todos.Count; i++)
            {
                Tipos_movimento tm = new Tipos_movimento(mov.tipos_movimento_id_todos[i]);
                Caixas caixa = new Caixas(mov.caixas_id_todos[i]);
                Usuarios usuarios = new Usuarios(mov.usuarios_id_todos[i]);
                Clientes clientes = new Clientes(mov.clientes_id_todos[i]);
                Pagamentos_movimentos pg_mov = new Pagamentos_movimentos(mov.pagamentos_movimentos_id_todos[i]);
                Formas_pagamento fp = new Formas_pagamento(pg_mov.Formas_pagamento_id);
                string parcelado = fp.Permitir_parcel == true ? parcelado = "SIM" : parcelado = "NÃO";
               
                dataGridMovimentos.Rows.Add(mov.id_todos[i], mov.data_movimento_todos[i], tm.Descricao, caixa.Nome, usuarios.Nome, clientes.Nome, pg_mov.Valor, pg_mov.Desconto, fp.Descricao, parcelado);

                decimal total = 0;
                foreach (DataGridViewRow row in dataGridMovimentos.Rows)
                {
                    decimal valor = (decimal)row.Cells[6].Value;
                    total += valor;
                }

                dtTmv.Rows.Add(mov.tipos_movimento_id_todos[i], tm.Descricao, pg_mov.Valor);

                lbTotal.Text = total.ToString("N2");
            }

            notif.Text = "Carregamento concluido.";
            cbExibir.Enabled = true;
        }

        private void cbExibir_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbExibir.Text == "Apenas este mês")
            {
                mov = new Movimentos();
                mov.Pesquisar("id", "%", 1);
            }
            else {
                mov = new Movimentos(true);
            }
              AtualizaDataGrid();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            AtualizaDataGrid();
            timer1.Stop();
        }

        private void txPesquisa_TextChanged(object sender, EventArgs e)
        {
            foreach(DataGridViewRow row in dataGridMovimentos.Rows)
            {
                if (row.Cells[Coluna()].Value.ToString().Contains(txPesquisa.Text))
                {
                    row.Visible = true;
                }
                else
                {
                    row.Visible = false;
                }
            }

            decimal total = 0;

            foreach (DataGridViewRow row in dataGridMovimentos.Rows)
            {
                if (row.Visible)
                {
                    total += decimal.Parse(row.Cells[6].Value.ToString());
                    lbTotal.Text = total.ToString();
                }
            }
        }

        private int Coluna()
        {
            switch (cbFiltro.Text)
            {
                case "Cod":
                    return 0;
                case "Data":
                    return 1;
                case "Tipo de movimento":
                    return 2;
                case "Caixa":
                    return 3;
                case "Usuário":
                    return 4;
                case "Cliente":
                    return 5;
                case "Forma de pagamento":
                    return 8;
            }
            return 0;
        }

        private void dataGridMovimentos_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (!dataGridMovimentos.TemLinhas()) return;

            int id = dataGridMovimentos.IdAtual(0);
            DetalhesMovimento dm = new DetalhesMovimento(id);
            dm.ShowDialog();
        }

        private void lbComparar_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            dtTmv.TableName = "tipos_movimento";
            DataSet ds = new DsTmv_grafico();
            ds.Tables["tipos_movimento"].Merge(dtTmv);

            DataView dv = dtTmv.DefaultView;
            dv.Sort = "id desc";
            DataTable sortedDT = dv.ToTable();

            DataTable dtValores = new DataTable();
            dtValores.Columns.Add("descricao");
            dtValores.Columns.Add("total", typeof(decimal));

            int tmvAtual = 0;
            decimal total = 0;
            foreach(DataRow row in sortedDT.Rows)
            {
                int idTmv =  int.Parse(row[0].ToString());
                if(idTmv != tmvAtual)
                {
                    Tipos_movimento tipo_mov = new Tipos_movimento(idTmv);
                    dtValores.Rows.Add(tipo_mov.Descricao, total);
                    total = 0;
                }
                total += decimal.Parse(row[2].ToString());
                tmvAtual = idTmv;
            }

            ReportDataSource rds_valores = new ReportDataSource();
            rds_valores.Name = "valores";
            rds_valores.Value = dtValores;

            ReportDataSource rds_tmv = new ReportDataSource();
            rds_tmv.Name = "tipos_movimento";
            rds_tmv.Value = ds.Tables["tipos_movimento"];
            new ExibeRelatorio("Relatorios/reports/TMV_Grafico.rdlc", new List<ReportDataSource>() { rds_tmv, rds_valores });
        }
    }
}

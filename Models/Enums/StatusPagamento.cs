namespace APIseverino.Models.Enums
{
    public enum StatusPagamento
    {
        Pendente = 0,

        /// <summary>
        /// Cliente pagou. Valor retido. Aguardando conclusão do serviço.
        /// </summary>
        Retido = 1,

        /// <summary>
        /// Serviço concluído. Valor liberado/transferido ao prestador.
        /// </summary>
        Liberado = 2,

        /// <summary>
        /// Cancelado por acordo mútuo ou por expiração do prazo limite.
        /// Reembolso emitido ao cliente.
        /// </summary>
        Cancelado = 3,

        /// <summary>
        /// Reembolso processado ao cliente com sucesso.
        /// </summary>
        Reembolsado = 4
    }
}
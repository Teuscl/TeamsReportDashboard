import axios from 'axios'; // 👈 Alterado de require para import
import dadosJson from './dados_atendimentos.js';
import https from 'https';
// URL da sua API rodando localmente
const API_URL = 'https://localhost:7258'; // Ajuste a porta se necessário

// 👇 2. Crie um "agente https" que diz ao Node.js para não rejeitar certificados autoassinados
const httpsAgent = new https.Agent({
  rejectUnauthorized: false,
});

// Função auxiliar para garantir que o formato de tempo seja HH:mm:ss
const formatTimeSpan = (timeStr) => {
    if (!timeStr) return '00:00:00';
    const parts = timeStr.split(':');
    while (parts.length < 3) {
        parts.unshift('0');
    }
    return parts.map(p => p.padStart(2, '0')).join(':');
};

// Função principal que executa o processo
async function seedReports() {
    console.log('Iniciando script de teste da API...');
    let successCount = 0;
    let errorCount = 0;

    // 1. Achatamos a lista de dados JSON
    const allAtendimentos = dadosJson.flatMap(jsonString => {
        try {
            const obj = JSON.parse(jsonString);
            return obj.atendimentos || [obj]; // Lida com ambos os formatos
        } catch (e) {
            console.error('Erro ao parsear um bloco JSON:', e.message);
            return [];
        }
    }).filter(atendimento => atendimento.email_solicitante && atendimento.email_solicitante.includes('@')); // Filtra registros inválidos

    console.log(`Total de ${allAtendimentos.length} atendimentos para processar.`);

    // 2. Iteramos sobre cada atendimento e enviamos para a API
    for (const atendimento of allAtendimentos) {
        const requestDate = `${atendimento.data_solicitacao}T${atendimento.hora_primeira_mensagem}`;

        const payload = {
            requesterName: atendimento.quem_solicitou_atendimento,
            requesterEmail: atendimento.email_solicitante,
            technicianName: Array.isArray(atendimento.quem_respondeu) ? atendimento.quem_respondeu.join(', ') : atendimento.quem_respondeu,
            requestDate: new Date(requestDate).toISOString(),
            reportedProblem: atendimento.problema_relatado,
            category: atendimento.categoria,
            firstResponseTime: formatTimeSpan(atendimento.tempo_primeira_resposta),
            averageHandlingTime: formatTimeSpan(atendimento.tempo_total_atendimento),
        };

        try {
            // Chamada POST para o endpoint de criação de relatórios
            await axios.post(`${API_URL}/report`, payload ,  { httpsAgent });
            console.log(`✅ Sucesso ao criar relatório para: ${payload.requesterEmail}`);
            successCount++;
        } catch (error) {
            console.error(`❌ Erro ao criar relatório para: ${payload.requesterEmail} - ${error.response?.data?.message || error.message}`);
            errorCount++;
        }
        
        // Pequeno delay para não sobrecarregar a API
        await new Promise(res => setTimeout(res, 50));
    }

    console.log('\n--- Script finalizado ---');
    console.log(`Total de sucessos: ${successCount}`);
    console.log(`Total de erros: ${errorCount}`);
}

seedReports();
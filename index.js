const express = require('express');
const cors = require('cors');
const { ServiceBusClient } = require('@azure/service-bus');
require('dotenv').config();
const app = express();

app.use(express.json());
app.use(cors());

app.post('/api/rent', async (req, res) => {
    console.log('Headers:', req.headers);
    console.log('Raw Body:', req.body);

    const {name, mail, model, year, rentTime } = req.body || {};
    
    // Validate required fields
    if (!name || !mail || !model || !year) {
        return res.status(400).json({ 
            error: 'Missing required fields',
            received: req.body 
        });
    }

    const connectionString = process.env.AZURE_SERVICE_BUS_CONNECTION_STRING;

    const rentMessage = {
        name,
        mail,
        model,
        year,
        rentTime,
        date: new Date().toISOString(),
    };

    try {
        // Validate connection string
        if (!connectionString) {
            throw new Error('Service Bus connection string is not configured');
        }

        // Create Service Bus client directly with connection string
        const sbClient = new ServiceBusClient(connectionString);
        const queueName = process.env.AZURE_SERVICE_BUS_QUEUE_NAME; 
        const sender = sbClient.createSender(queueName);

        console.log('Attempting to send message to queue:', queueName);
        
        await sender.sendMessages({
            body: rentMessage,
            contentType: 'application/json',
            label: 'rent'
        });

        await sender.close();
        await sbClient.close();

        res.status(201).json({ message: 'rent request sent for queue successfully' });
    } catch (error) {
        console.error('Error details:', {
            message: error.message,
            code: error.code,
            details: error.info
        });
        return res.status(500).json({ 
            error: 'Failed to send message',
            details: error.message 
        });
    }
});

app.listen(process.env.PORT || 3000, () => {
    console.log(`Server is running on port ${process.env.PORT || 3000}`);
});
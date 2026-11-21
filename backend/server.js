
import express from 'express';
import dotenv from 'dotenv';
import cors from 'cors';
import mongoose from 'mongoose';
import dns from 'node:dns';
import authRoutes from './routes/authRoutes.js'

dotenv.config();

dns.setServers(['8.8.8.8', '1.1.1.1']);

const app = express();

// Middleware
app.use(cors());
app.use(express.json());
app.use('/api/auth', authRoutes);

// Test Route
app.get('/', (req, res) => {
  res.status(200).json({
    success: true,
    message: 'Ledger-Guard Backend is running!'
  });
});

// MongoDB Connection
mongoose.connect(process.env.MONGO_URI)
  .then(() => {
    console.log('MongoDB Atlas Connected Successfully!');
    const PORT = process.env.PORT || 5000;

    app.listen(PORT, () => {
      console.log(`Server running on port ${PORT}`);
    });
  })
  .catch((error)=>{
    console.log('MongoDB Connection Failed:', error.message);
  })